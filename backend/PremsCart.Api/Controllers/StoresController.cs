using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PremsCart.Api.Authentication;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

public sealed record StoreProfileRequest(string StoreName, string? Description);
public sealed record StockRequest(int ProductId, int Quantity);
public sealed record QuantityRequest(int Quantity);
public sealed record StoreDisplayRequest(bool IsVisible);
public sealed record StoreOrderRequest(List<int> ProductIds);

[ApiController]
[Authorize]
[Route("api/stores")]
public sealed class StoresController(PremsCartDbContext db, JwtTokenService tokens) : ControllerBase
{
    private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Browse([FromQuery] int page = 1, [FromQuery] string? search = null)
    {
        if (page is < 1 or > 10000) return BadRequest(new { error = "Invalid page." });
        if (search?.Length > 100) return BadRequest(new { error = "Search is too long." });
        var query = db.Stores.AsNoTracking().Where(s => !s.IsHidden && s.Owner.IsVerified && s.Owner.Status == "Active" && s.Owner.Role.RoleName == "Business Seller");
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(s => EF.Functions.ILike(s.StoreName, $"%{search.Trim()}%"));
        return Ok(new
        {
            total = await query.CountAsync(),
            page,
            items = await query.OrderBy(s => s.StoreName).Skip((page - 1) * 12).Take(12).Select(s => new
            {
                s.Id, s.StoreName, s.Description, s.Logo, s.OwnerId,
                OwnerName = s.Owner.FirstName + " " + s.Owner.LastName,
                OwnerVerified = s.Owner.IsVerified,
                Rating = db.Reviews.Where(r => r.ReviewedUserId == s.OwnerId && !r.IsHidden).Average(r => (double?)r.Rating),
                ReviewCount = db.Reviews.Count(r => r.ReviewedUserId == s.OwnerId && !r.IsHidden),
                ProductCount = s.StoreProducts.Count(p => p.IsVisible && p.Quantity > 0 && p.Product.Status == "Available" && !p.Product.IsHidden)
            }).ToListAsync()
        });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var store = await db.Stores.AsNoTracking().Where(s => s.OwnerId == Me)
            .Select(s => new { s.Id, s.StoreName, s.Description, s.Logo, s.OwnerId }).SingleOrDefaultAsync();
        return store is null ? NotFound(new { error = "Create a store first." }) : Ok(store);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var store = await db.Stores.AsNoTracking().Where(s => s.Id == id && !s.IsHidden && s.Owner.IsVerified && s.Owner.Status == "Active" && s.Owner.Role.RoleName == "Business Seller")
            .Select(s => new
            {
                s.Id, s.StoreName, s.Description, s.Logo, s.OwnerId,
                OwnerName = s.Owner.FirstName + " " + s.Owner.LastName,
                OwnerVerified = s.Owner.IsVerified,
                Rating = db.Reviews.Where(r => r.ReviewedUserId == s.OwnerId && !r.IsHidden).Average(r => (double?)r.Rating),
                ReviewCount = db.Reviews.Count(r => r.ReviewedUserId == s.OwnerId && !r.IsHidden),
                Products = s.StoreProducts
                    .Where(sp => sp.IsVisible && sp.Quantity > 0 && sp.Product.Status == "Available" && !sp.Product.IsHidden)
                    .OrderBy(sp => sp.SortOrder).ThenByDescending(sp => sp.Product.CreatedAt)
                    .Select(sp => new
                    {
                        sp.ProductId, sp.Quantity, sp.SortOrder,
                        sp.Product.Title, sp.Product.Price, sp.Product.AllowRent, sp.Product.RentalPrice,
                        sp.Product.TransactionType, sp.Product.Condition, sp.Product.Location,
                        CategoryName = sp.Product.Category.CategoryName,
                        ImageUrl = sp.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault()
                    }).ToList()
            }).SingleOrDefaultAsync();
        return store is null ? NotFound() : Ok(store);
    }

    [HttpGet("mine/summary")]
    public async Task<IActionResult> Summary() => Ok(new
    {
        itemsSold = await db.Orders.CountAsync(x => x.SellerId == Me && x.Status == "Completed" && x.Product.TransactionType == "Sell"),
        totalSales = await db.Orders.Where(x => x.SellerId == Me && x.Status == "Completed" && x.Product.TransactionType == "Sell").SumAsync(x => x.FinalPrice) ?? 0,
        visibleItems = await db.StoreProducts.CountAsync(x => x.Store.OwnerId == Me && x.IsVisible && x.Quantity > 0 && x.Product.Status == "Available" && !x.Product.IsHidden)
    });

    [HttpGet("mine/products")]
    public async Task<IActionResult> MyProducts()
    {
        var storeId = await db.Stores.Where(s => s.OwnerId == Me).Select(s => (int?)s.Id).SingleOrDefaultAsync();
        if (storeId is null) return NotFound(new { error = "Create a store first." });
        return Ok(await db.StoreProducts.AsNoTracking().Where(sp => sp.StoreId == storeId)
            .OrderBy(sp => sp.SortOrder).ThenByDescending(sp => sp.Product.CreatedAt)
            .Select(sp => new
            {
                sp.ProductId, sp.Quantity, sp.IsVisible, sp.SortOrder,
                sp.Product.Title, sp.Product.Price, sp.Product.AllowRent, sp.Product.RentalPrice,
                sp.Product.TransactionType, sp.Product.Status, sp.Product.Condition, sp.Product.Location,
                CategoryName = sp.Product.Category.CategoryName,
                ImageUrl = sp.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault()
            }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> Create(StoreProfileRequest input)
    {
        var error = Validate(input);
        if (error != null) return BadRequest(new { error });
        var owner = await db.Users.Include(u => u.Role).SingleAsync(u => u.Id == Me);
        if (!owner.IsVerified) return Forbid();
        if (await db.Stores.AnyAsync(s => s.OwnerId == Me)) return Conflict(new { error = "You already have a store." });

        var store = new Store { OwnerId = Me, StoreName = input.StoreName.Trim(), Description = input.Description?.Trim() };
        db.Stores.Add(store);
        owner.RoleId = 2;
        owner.Role = await db.Roles.SingleAsync(r => r.RoleName == "Business Seller");
        owner.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { return Conflict(new { error = "You already have a store." }); }

        // A store is a presentation layer for the owner's posts. Existing posts appear automatically.
        var productIds = await db.Products.Where(p => p.SellerId == Me).OrderByDescending(p => p.CreatedAt).Select(p => p.Id).ToListAsync();
        for (var i = 0; i < productIds.Count; i++)
            db.StoreProducts.Add(new StoreProduct { StoreId = store.Id, ProductId = productIds[i], Quantity = 1, IsVisible = true, SortOrder = i });
        if (productIds.Count > 0) await db.SaveChangesAsync();

        return Created($"/api/stores/{store.Id}", new { id = store.Id, token = tokens.Create(owner) });
    }

    [HttpPut("mine")]
    [Authorize(Roles = "Business Seller")]
    public async Task<IActionResult> Update(StoreProfileRequest input)
    {
        var error = Validate(input);
        if (error != null) return BadRequest(new { error });
        var store = await db.Stores.SingleOrDefaultAsync(s => s.OwnerId == Me);
        if (store is null) return NotFound();
        store.StoreName = input.StoreName.Trim();
        store.Description = input.Description?.Trim();
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Kept for compatibility and recovery. Normal seller posts are added automatically in ProductsController.
    [HttpPost("mine/products")]
    [Authorize(Roles = "Business Seller")]
    public async Task<IActionResult> AddProduct(StockRequest input)
    {
        if (input.Quantity < 1 || input.Quantity > 10000) return BadRequest(new { error = "Quantity must be from 1 to 10,000." });
        var store = await db.Stores.SingleOrDefaultAsync(s => s.OwnerId == Me);
        if (store is null) return NotFound(new { error = "Create a store first." });
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == input.ProductId);
        if (product is null) return NotFound(new { error = "Item not found." });
        if (product.SellerId != Me) return Forbid();
        if (await db.StoreProducts.AnyAsync(sp => sp.ProductId == product.Id)) return Conflict(new { error = "This item is already in your store." });
        var next = (await db.StoreProducts.Where(sp => sp.StoreId == store.Id).MaxAsync(sp => (int?)sp.SortOrder) ?? -1) + 1;
        db.StoreProducts.Add(new StoreProduct { StoreId = store.Id, ProductId = product.Id, Quantity = input.Quantity, IsVisible = true, SortOrder = next });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { return Conflict(new { error = "This item is already in your store." }); }
        return Created($"/api/stores/{store.Id}", new { product.Id, input.Quantity });
    }

    [HttpPut("mine/products/{productId:int}")]
    [Authorize(Roles = "Business Seller")]
    public async Task<IActionResult> SetStock(int productId, QuantityRequest input)
    {
        if (input.Quantity < 0 || input.Quantity > 10000) return BadRequest(new { error = "Stock must be from 0 to 10,000." });
        var item = await db.StoreProducts.Include(sp => sp.Product).Include(sp => sp.Store)
            .SingleOrDefaultAsync(sp => sp.ProductId == productId && sp.Store.OwnerId == Me);
        if (item is null) return NotFound();
        if (item.Product.IsHidden) return Conflict(new { error = "This item is hidden by administration." });
        if (item.Product.TransactionType == "Rent" || item.Product.AllowRent) return BadRequest(new { error = "Rental items do not use store stock." });
        item.Quantity = input.Quantity;
        if (item.Product.Status is "Available" or "Reserved" ||
            (item.Product.Status == "Unavailable" || item.Product.Status == "Sold" || item.Product.Status == "GivenAway") && input.Quantity > 0)
            item.Product.Status = input.Quantity == 0 ? "Unavailable" : "Available";
        await db.SaveChangesAsync();
        return Ok(new { item.ProductId, item.Quantity });
    }

    [HttpPut("mine/products/{productId:int}/display")]
    [Authorize(Roles = "Business Seller")]
    public async Task<IActionResult> SetDisplay(int productId, StoreDisplayRequest input)
    {
        var item = await db.StoreProducts.Include(sp => sp.Store).SingleOrDefaultAsync(sp => sp.ProductId == productId && sp.Store.OwnerId == Me);
        if (item is null) return NotFound();
        item.IsVisible = input.IsVisible;
        await db.SaveChangesAsync();
        return Ok(new { item.ProductId, item.IsVisible });
    }

    [HttpPut("mine/products/order")]
    [Authorize(Roles = "Business Seller")]
    public async Task<IActionResult> Reorder(StoreOrderRequest input)
    {
        var store = await db.Stores.SingleOrDefaultAsync(s => s.OwnerId == Me);
        if (store is null) return NotFound(new { error = "Create a store first." });
        var items = await db.StoreProducts.Where(sp => sp.StoreId == store.Id).ToListAsync();
        var ids = input.ProductIds.Distinct().ToList();
        if (ids.Count != items.Count || items.Any(x => !ids.Contains(x.ProductId)))
            return BadRequest(new { error = "Refresh the page and try sorting again." });
        var position = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        foreach (var item in items) item.SortOrder = position[item.ProductId];
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("mine/products/{productId:int}")]
    [Authorize(Roles = "Business Seller")]
    public async Task<IActionResult> RemoveProduct(int productId)
    {
        var item = await db.StoreProducts.Include(sp => sp.Store).SingleOrDefaultAsync(sp => sp.ProductId == productId && sp.Store.OwnerId == Me);
        if (item is null) return NotFound();
        if (await db.Orders.AnyAsync(o => o.ProductId == productId && (o.Status == "Accepted" || o.Status == "Pickup scheduled")))
            return Conflict(new { error = "Complete or cancel active orders first." });
        db.StoreProducts.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static string? Validate(StoreProfileRequest input) =>
        string.IsNullOrWhiteSpace(input.StoreName) || input.StoreName.Trim().Length > 100 || input.Description?.Length > 1000
            ? "Store name is required (up to 100 characters); description is up to 1,000 characters." : null;
}
