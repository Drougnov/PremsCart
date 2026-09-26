using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;
namespace PremsCart.Api.Controllers;
public record CartLine(int ProductId, int? RentalDays = null, decimal? ExpectedUnitPrice = null, DateOnly? RentalStartDate = null, string? Mode = null);
public record CartRequest(List<CartLine> Items);
[ApiController, Authorize(Roles = "Student,Business Seller"), Route("api/cart")]
public sealed class CartController(PremsCartDbContext db) : ControllerBase {
    int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    static readonly string[] Active = ["Pending", "Accepted", "Pickup scheduled", "Rented", "Return requested"];
    static bool Valid(CartRequest input) => input.Items is { Count: > 0 and <= 20 } && input.Items.All(x => x != null && x.ProductId > 0) && input.Items.Select(x => x.ProductId).Distinct().Count() == input.Items.Count;
    static string Mode(Product p, CartLine line)=>line.Mode ?? (p.TransactionType=="Rent" || (p.AllowRent && line.RentalDays.HasValue) ? "Rent" : p.TransactionType);
    static decimal Unit(Product p, CartLine line)=>Mode(p,line)=="Giveaway" ? 0 : Mode(p,line)=="Rent" && p.AllowRent ? p.RentalPrice??0 : p.Price??0;
    async Task<string?> Unavailable(Product p, CartLine line) {
        var mode=Mode(p,line);
        if (mode != p.TransactionType && !(mode=="Rent" && p.AllowRent)) return "This transaction option is not available.";
        if (p.SellerId == Me) return "This is your own item.";
        if (p.IsHidden || p.Status != "Available" || p.Seller.Status != "Active" || !p.Seller.IsVerified) return "This item is no longer available.";
        if (await db.StoreProducts.AnyAsync(x => x.ProductId == p.Id && (x.Quantity < 1 || x.Store.IsHidden))) return "This store item is unavailable.";
        if (p.TransactionType is not ("Sell" or "Rent" or "Giveaway")) return "This item type is not supported.";
        if (Mode(p,line) == "Rent" && line.RentalDays is not (>= 1 and <= 30)) return "Choose a rental duration of 1–30 days.";
        if (line.RentalStartDate.HasValue && (Mode(p,line) != "Rent" || line.RentalStartDate < DateOnly.FromDateTime(DateTime.UtcNow) || line.RentalStartDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)))) return "Choose a preferred rental pickup date within the next 90 days.";
        if (Mode(p,line) != "Rent" && line.RentalDays != null) return "Only rental items accept a duration.";
        if (p.TransactionType != "Giveaway" && (p.Price is null or <= 0)) return "This item needs a valid price.";
        if (Unit(p,line) * (Mode(p,line) == "Rent" ? line.RentalDays ?? 1 : 1) > 9999999999.99m) return "This total exceeds the supported amount. Choose fewer days.";
        if (await db.Orders.AnyAsync(x => x.ProductId == p.Id && x.BuyerId == Me && Active.Contains(x.Status)) || await db.Offers.AnyAsync(x => x.ProductId == p.Id && x.BuyerId == Me && (x.Status == "Pending" || x.Status == "Countered"))) return "You already have an active request or offer for this item.";
        return null;
    }
    [HttpPost("quote")]
    public async Task<IActionResult> Quote(CartRequest input) {
        if (!Valid(input)) return BadRequest(new { error = "Choose 1–20 distinct items." });
        var ids = input.Items.Select(x => x.ProductId).ToList();
        var products = await db.Products.Include(x => x.Seller).Include(x => x.Images).Where(x => ids.Contains(x.Id)).ToListAsync();
        var rows = new List<object>();
        foreach (var line in input.Items) {
            var p = products.SingleOrDefault(x => x.Id == line.ProductId);
            if (p == null) { rows.Add(new { id = line.ProductId, title = "Removed item", available = false, error = "This item no longer exists." }); continue; }
            var error = await Unavailable(p,line);
            var unit = Unit(p,line);
            rows.Add(new { p.Id, p.Title, TransactionType = Mode(p,line), p.SellerId, sellerName = p.Seller.FirstName + " " + p.Seller.LastName, p.Location, unitPrice = unit, line.RentalDays, line.RentalStartDate, total = unit * (Mode(p,line) == "Rent" ? line.RentalDays ?? 1 : 1), imageUrl = p.Images.OrderByDescending(x => x.IsPrimary).Select(x => x.ImageUrl).FirstOrDefault(), available = error == null, error });
        }
        return Ok(new { items = rows });
    }
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CartRequest input) {
        if (!Valid(input)) return BadRequest(new { error = "Choose 1–20 distinct items." });
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var orders = new List<Order>();
        foreach (var line in input.Items) {
            var p = await db.Products.Include(x => x.Seller).SingleOrDefaultAsync(x => x.Id == line.ProductId);
            if (p == null) return Conflict(new { error = "An item was removed. Refresh your cart." });
            var error = await Unavailable(p,line); if (error != null) return Conflict(new { error = p.Title + ": " + error });
            var unit = Unit(p,line);
            if (line.ExpectedUnitPrice != unit) return Conflict(new { error = "A price changed. Refresh your cart and review the new total." });
            orders.Add(new Order { RentalStartDate = Mode(p,line) == "Rent" ? line.RentalStartDate : null, ProductId = p.Id, BuyerId = Me, SellerId = p.SellerId, RentalDays = Mode(p,line) == "Rent" ? line.RentalDays : null, FinalPrice = unit * (Mode(p,line) == "Rent" ? line.RentalDays!.Value : 1) });
        }
        db.Orders.AddRange(orders); await db.SaveChangesAsync(); await tx.CommitAsync();
        return Ok(new { orderIds = orders.Select(x => x.Id), message = "Requests sent. Sellers will confirm availability; arrange payment and pickup with each seller." });
    }
}
