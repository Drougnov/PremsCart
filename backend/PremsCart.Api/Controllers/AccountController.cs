using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Authentication;
using PremsCart.Api.Data;
using PremsCart.Api.Models;
namespace PremsCart.Api.Controllers;
public record ResetPasswordRequest([Required] string Email, [Required] string Code, [Required, MinLength(8), MaxLength(128)] string Password);
public record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(8), MaxLength(128)] string Password);
[ApiController, Authorize]
public class AccountController(PremsCartDbContext db, IPasswordHasher<User> hasher, VerificationEmailSender sender, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    string Hash(int id, string code) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(config["Jwt:Key"]!), Encoding.UTF8.GetBytes($"reset:{id}:{code}")));
    [AllowAnonymous, HttpPost("api/auth/forgot-password")]
    public async Task<IActionResult> Forgot(EmailRequest input) {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == input.Email.Trim().ToLower());
        if (user != null && user.IsVerified && user.Status == "Active") {
            var reset = await db.PasswordResets.SingleOrDefaultAsync(x => x.UserId == user.Id);
            if (reset == null || reset.CreatedAt < DateTime.UtcNow.AddMinutes(-1)) {
                reset ??= new PasswordReset { UserId = user.Id };
                var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                reset.CodeHash = Hash(user.Id, code); reset.CreatedAt = DateTime.UtcNow; reset.ExpiresAt = DateTime.UtcNow.AddMinutes(10); reset.FailedAttempts = 0;
                if (reset.Id == 0) db.PasswordResets.Add(reset);
                await sender.SendAsync(user.Email, code, "password reset"); await db.SaveChangesAsync();
            }
        }
        return Ok(new { message = "If an eligible account exists, a reset code has been sent." });
    }
    [AllowAnonymous, HttpPost("api/auth/reset-password")]
    public async Task<IActionResult> Reset(ResetPasswordRequest input) {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == input.Email.Trim().ToLower());
        var reset = user == null ? null : await db.PasswordResets.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (user == null || user.Status != "Active" || reset == null || reset.ExpiresAt < DateTime.UtcNow || reset.FailedAttempts >= 5) return BadRequest(new { error = "Invalid or expired reset code." });
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(reset.CodeHash), Convert.FromHexString(Hash(user.Id, input.Code)))) {
            reset.FailedAttempts++; await db.SaveChangesAsync(); return BadRequest(new { error = "Invalid or expired reset code." });
        }
        user.PasswordHash = hasher.HashPassword(user, input.Password); user.TokenVersion++; db.PasswordResets.Remove(reset); await db.SaveChangesAsync(); return Ok(new { message = "Password reset. Sign in again." });
    }
    [HttpPost("api/auth/change-password")]
    public async Task<IActionResult> Change(ChangePasswordRequest input) {
        var u = await db.Users.FindAsync(Me);
        if (hasher.VerifyHashedPassword(u!, u!.PasswordHash, input.CurrentPassword) == PasswordVerificationResult.Failed) return BadRequest(new { error = "Current password is incorrect." });
        u.PasswordHash = hasher.HashPassword(u, input.Password); u.TokenVersion++;
        db.PasswordResets.RemoveRange(await db.PasswordResets.Where(x => x.UserId == Me).ToListAsync());
        await db.SaveChangesAsync(); return Ok(new { message = "Password changed. Sign in again." });
    }
    [HttpGet("api/users/{id:int}")]
    public async Task<IActionResult> PublicProfile(int id) {
        var u = await db.Users.Where(x => x.Id == id && x.IsVerified).Select(x => new { x.Id, x.FirstName, x.LastName, x.ProfileImage, x.IsVerified, x.Batch, x.Department, x.CreatedAt }).SingleOrDefaultAsync();
        if (u == null) return NotFound();
        return Ok(new { profile = u, completed = await db.Orders.CountAsync(x => (x.BuyerId == id || x.SellerId == id) && x.Status == "Completed"), sold = await db.Orders.CountAsync(x => x.SellerId == id && x.Status == "Completed" && !x.RentalDays.HasValue && x.Product.TransactionType == "Sell"), givenAway = await db.Orders.CountAsync(x => x.SellerId == id && x.Status == "Completed" && x.Product.TransactionType == "Giveaway"), listings = await db.Products.Where(x => x.SellerId == id && x.Status == "Available" && !x.IsHidden && !db.StoreProducts.Any(sp => sp.ProductId == x.Id && sp.Store.IsHidden)).Select(x => new { x.Id, x.Title, x.Price, x.AllowRent, x.RentalPrice, x.TransactionType }).ToListAsync() });
    }
    [HttpPost("api/users/profile/image"), HttpPost("api/stores/mine/logo"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Image([FromForm] IFormFile file) {
        var storeUpload = Request.Path.Value!.Contains("stores");
        var store = storeUpload ? await db.Stores.SingleOrDefaultAsync(x => x.OwnerId == Me) : null;
        if (storeUpload && store == null) return NotFound();
        if (file.Length < 12 || file.Length > 5 * 1024 * 1024) return BadRequest(new { error = "Choose a JPEG, PNG or WebP under 5 MB." });
        using var stream = file.OpenReadStream(); var h = new byte[12]; await stream.ReadExactlyAsync(h);
        var ext = h[0] == 255 && h[1] == 216 && h[2] == 255 ? "jpg" : h.AsSpan(0,8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ? "png" : h.AsSpan(0,4).SequenceEqual("RIFF"u8) && h.AsSpan(8,4).SequenceEqual("WEBP"u8) ? "webp" : null;
        if (ext == null) return BadRequest(new { error = "Unsupported image." });
        var name = $"{Guid.NewGuid():N}.{ext}"; var dir = Path.Combine(env.ContentRootPath, "uploads"); Directory.CreateDirectory(dir);
        using (var output = System.IO.File.Create(Path.Combine(dir,name))) { await output.WriteAsync(h); await stream.CopyToAsync(output); }
        var url = $"/api/account-images/{name}";
        if (store != null) store.Logo = url; else (await db.Users.FindAsync(Me))!.ProfileImage = url;
        await db.SaveChangesAsync(); return Ok(new { imageUrl = url });
    }
    [HttpGet("api/account-images/{name}")]
    public IActionResult ImageFile(string name) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-f0-9]{32}\.(jpg|png|webp)$")) return NotFound();
        var path = Path.Combine(env.ContentRootPath,"uploads",name); if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, name.EndsWith("png") ? "image/png" : name.EndsWith("webp") ? "image/webp" : "image/jpeg");
    }
    [HttpGet("api/notifications")]
    public async Task<IActionResult> Notifications() => Ok(new { unread = await db.Notifications.CountAsync(x => x.UserId == Me && !x.IsRead), items = await db.Notifications.Where(x => x.UserId == Me).OrderByDescending(x => x.Id).Take(100).Select(x => new { x.Id, x.Title, x.Message, x.Link, x.IsRead, x.CreatedAt }).ToListAsync() });
    [HttpPost("api/notifications/{id:int}/read")]
    public async Task<IActionResult> Read(int id) { var n = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == Me); if (n == null) return NotFound(); n.IsRead = true; await db.SaveChangesAsync(); return Ok(); }
    [HttpPost("api/notifications/read-all")]
    public async Task<IActionResult> ReadAll() { await db.Notifications.Where(x => x.UserId == Me).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead,true)); return Ok(); }
    [HttpGet("api/dashboard/student")]
    public async Task<IActionResult> Dashboard() => Ok(new {
        activeListings = await db.Products.CountAsync(x => x.SellerId == Me && x.Status == "Available" && !x.IsHidden),
        pendingSales = await db.Orders.CountAsync(x => x.SellerId == Me && x.Status == "Pending"),
        activeRentals = await db.Orders.CountAsync(x => (x.BuyerId == Me || x.SellerId == Me) && (x.Status == "Rented" || x.Status == "Return requested")),
        offersToRespond = await db.Offers.CountAsync(x => (x.BuyerId == Me || x.SellerId == Me) && (x.Status == "Pending" || x.Status == "Countered") && (x.LastProposerId ?? x.BuyerId) != Me),
        recentOrders = await db.Orders.Where(x => x.BuyerId == Me || x.SellerId == Me).OrderByDescending(x => x.Id).Take(5).Select(x => new { x.Id, title = x.Product.Title, x.Status, TransactionType = x.RentalDays.HasValue ? "Rent" : x.Product.TransactionType, x.RentalDueAt }).ToListAsync(),
        sold = await db.Orders.CountAsync(x => x.SellerId == Me && x.Status == "Completed" && !x.RentalDays.HasValue && x.Product.TransactionType == "Sell"),
        givenAway = await db.Orders.CountAsync(x => x.SellerId == Me && x.Status == "Completed" && x.Product.TransactionType == "Giveaway"),
        pendingPurchases = await db.Orders.CountAsync(x => x.BuyerId == Me && x.Status == "Pending"),
        unreadMessages = await db.Messages.CountAsync(x => (x.Conversation.BuyerId == Me || x.Conversation.SellerId == Me) && x.SenderId != Me && !x.IsRead),
        notifications = await db.Notifications.CountAsync(x => x.UserId == Me && !x.IsRead), wishlist = await db.Wishlist.CountAsync(x => x.UserId == Me),
        rating = await db.Reviews.Where(x => x.ReviewedUserId == Me && !x.IsHidden).AverageAsync(x => (double?)x.Rating),
        pickups = await db.Orders.Where(x => (x.BuyerId == Me || x.SellerId == Me) && x.Status == "Pickup scheduled").OrderBy(x => x.PickupTime).Take(5).Select(x => new { x.Id, title = x.Product.Title, x.PickupTime, x.PickupStatus }).ToListAsync()
    });
}
