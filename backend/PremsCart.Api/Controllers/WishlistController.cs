using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wishlist")]
public sealed class WishlistController(PremsCartDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Wishlist.AsNoTracking().Where(w => w.UserId == CurrentUserId)
            .OrderByDescending(w => w.CreatedAt).Select(w => new
            {
                w.ProductId, w.CreatedAt,
                w.Product.Title, w.Product.Price, w.Product.AllowRent, w.Product.RentalPrice, w.Product.TransactionType,
                Status = w.Product.IsHidden || w.Product.Seller.Status != "Active" || db.StoreProducts.Any(sp => sp.ProductId == w.ProductId && sp.Store.IsHidden) ? "Unavailable" : w.Product.Status, w.Product.SellerId, w.Product.Condition, w.Product.Location, CategoryName = w.Product.Category.CategoryName, SellerVerified = w.Product.Seller.IsVerified,
                SellerName = w.Product.Seller.FirstName + " " + w.Product.Seller.LastName,
                ImageUrl = w.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            }).ToListAsync());

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Save(int productId)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.Status == "Available" && !p.IsHidden))
            return NotFound(new { error = "This item is unavailable." });
        if (await db.Wishlist.AnyAsync(x => x.UserId == CurrentUserId && x.ProductId == productId))
            return Ok(new { saved = true });
        db.Wishlist.Add(new WishlistItem { UserId = CurrentUserId, ProductId = productId });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Ok(new { saved = true });
        }
        return Ok(new { saved = true });
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var item = await db.Wishlist.SingleOrDefaultAsync(x =>
            x.UserId == CurrentUserId && x.ProductId == productId);
        if (item is null) return NoContent();
        db.Wishlist.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
