using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

public sealed record ProductRequest(
    [Required, StringLength(120)] string Title,
    [Required, StringLength(3000)] string Description,
    int CategoryId,
    decimal? Price,
    [Required] string TransactionType,
    [Required] string Condition,
    [Required] string Status,
    [StringLength(120)] string? Location,
    bool IsNegotiable, bool AllowRent = false, decimal? RentalPrice = null);

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(PremsCartDbContext db, IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] Types = ["Sell", "Giveaway", "Rent"];
    private static readonly string[] Conditions = ["New", "Like new", "Good", "Fair"];
    private static readonly string[] Statuses = ["Available", "Reserved", "Unavailable"];
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UploadDirectory => Path.Combine(environment.ContentRootPath, "uploads");

    [AllowAnonymous, HttpGet("categories")]
    public async Task<IActionResult> Categories() =>
        Ok(await db.Categories.AsNoTracking().OrderBy(x => x.CategoryName)
            .Select(x => new { x.Id, x.CategoryName }).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Browse(
        [FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] string? type,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] int page = 1, [FromQuery] string? condition = null, [FromQuery] bool? negotiable = null, [FromQuery] string? location = null, [FromQuery] string sort = "newest")
    {
        if (page < 1 || page > 10000 || minPrice < 0 || maxPrice < 0 || minPrice > maxPrice ||
            search?.Length > 100 || (type is not null && !Types.Contains(type)))
            return BadRequest(new { error = "Invalid search or filter." });
        var query = db.Products.AsNoTracking().Where(p => p.Status == "Available" && !p.IsHidden && p.Seller.Status == "Active" && p.Seller.IsVerified && !db.StoreProducts.Any(sp => sp.ProductId == p.Id && sp.Store.IsHidden));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Title, $"%{term}%") ||
                                     EF.Functions.ILike(p.Description, $"%{term}%"));
        }
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (type is not null) query = query.Where(p => (p.TransactionType == type || (type == "Rent" && p.AllowRent)));
        if (minPrice.HasValue) query = query.Where(p => (type == "Rent" && p.AllowRent ? p.RentalPrice : p.Price) >= minPrice);
        if (maxPrice.HasValue) query = query.Where(p => (type == "Rent" && p.AllowRent ? p.RentalPrice : p.Price) <= maxPrice);
        if (condition != null) query = query.Where(p => p.Condition == condition);
        if (negotiable.HasValue) query = query.Where(p => p.IsNegotiable == negotiable);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(p => p.Location == location);
        var total = await query.CountAsync();
        var sorted = sort == "price-low" ? query.OrderBy(p => type == "Rent" && p.AllowRent ? p.RentalPrice : p.Price).ThenBy(p => p.Id) : sort == "price-high" ? query.OrderByDescending(p => type == "Rent" && p.AllowRent ? p.RentalPrice : p.Price).ThenBy(p => p.Id) : query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
        var products = await sorted
            .Skip((page - 1) * 12).Take(12).Select(p => new
            {
                p.Id, p.Title, p.Price, p.AllowRent, p.RentalPrice, p.TransactionType, p.Condition, p.Status,
                p.Location, p.CreatedAt, p.SellerId, SellerVerified = p.Seller.IsVerified,
                SellerName = p.Seller.FirstName + " " + p.Seller.LastName,
                CategoryName = p.Category.CategoryName,
                ImageUrl = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            }).ToListAsync();
        return Ok(new { items = products, total, page, pageSize = 12 });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine() =>
        Ok(await db.Products.AsNoTracking().Where(p => p.SellerId == CurrentUserId)
            .OrderByDescending(p => p.CreatedAt).Select(p => new
            {
                p.Id, p.IsHidden, p.Title, p.Price, p.AllowRent, p.RentalPrice, p.Status, p.TransactionType, p.Condition, p.Location, p.SellerId,
                ImageUrl = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                    .Select(i => i.ImageUrl).FirstOrDefault()
            }).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var product = await db.Products.AsNoTracking().Where(p => p.Id == id && (p.SellerId == CurrentUserId || p.Seller.Status == "Active"))
            .Select(p => new
            {
                p.Id, p.IsHidden, p.SellerId, p.Title, p.Description, p.Price, p.AllowRent, p.RentalPrice, p.TransactionType,
                p.Condition, p.Status, p.Location, p.IsNegotiable, p.CreatedAt, SellerVerified = p.Seller.IsVerified,
                p.CategoryId, CategoryName = p.Category.CategoryName,
                SellerName = p.Seller.FirstName + " " + p.Seller.LastName,
                StoreId = db.StoreProducts.Where(sp => sp.ProductId == p.Id)
                    .Select(sp => (int?)sp.StoreId).FirstOrDefault(),
                Images = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                    .Select(i => new { i.Id, i.ImageUrl, i.IsPrimary }).ToList()
            }).SingleOrDefaultAsync();
        if (product != null && product.SellerId != CurrentUserId && await db.StoreProducts.AnyAsync(x => x.ProductId == id && x.Store.IsHidden)) return NotFound();
        if (product is null || (product.IsHidden && product.SellerId != CurrentUserId) || (product.Status != "Available" && product.SellerId != CurrentUserId && !await db.Orders.AnyAsync(o=>o.ProductId==id && o.BuyerId==CurrentUserId)))
            return NotFound();
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> Create(ProductRequest request)
    {
        var error = await Validate(request);
        if (error is not null) return BadRequest(new { error });
        var product = new Product
        {
            SellerId = CurrentUserId, CategoryId = request.CategoryId,
            Title = request.Title.Trim(), Description = request.Description.Trim(),
            Price = request.TransactionType == "Giveaway" ? 0 : request.Price,
            AllowRent = request.AllowRent, RentalPrice = request.AllowRent ? request.RentalPrice : null,
            TransactionType = request.TransactionType, Condition = request.Condition,
            Status = request.Status, Location = request.Location?.Trim(),
            IsNegotiable = request.IsNegotiable && request.TransactionType == "Sell"
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Details), new { id = product.Id }, new { product.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        if (product.SellerId != CurrentUserId) return Forbid();
        if (product.IsHidden) return Conflict(new { error = "This listing is hidden by administration." });
        if (request.Status == "Available" && await db.StoreProducts.AnyAsync(sp => sp.ProductId == id && sp.Quantity == 0))
            return Conflict(new { error = "Restock this product from your store before making it available." });
        if (product.Status is "Sold" or "GivenAway" || await db.Orders.AnyAsync(o => o.ProductId == id && (o.Status == "Pending" || o.Status == "Accepted" || o.Status == "Pickup scheduled" || o.Status == "Rented" || o.Status == "Return requested")))
            return Conflict(new { error = "Finish or cancel active transactions before editing this listing." });
        var error = await Validate(request);
        if (error is not null) return BadRequest(new { error });
        if ((request.TransactionType == "Rent" || request.AllowRent) && await db.StoreProducts.AnyAsync(x => x.ProductId == id)) return Conflict(new { error = "Remove this item from store inventory before converting it to a rental." });
        if (product.TransactionType != request.TransactionType && await db.Orders.AnyAsync(x => x.ProductId == id)) return Conflict(new { error = "Create a new listing to change transaction type after an order." });
        if (await db.Offers.AnyAsync(x => x.ProductId == id && (x.Status == "Pending" || x.Status == "Countered"))) return Conflict(new { error = "Close active offers before editing listing terms." });
        product.Title = request.Title.Trim();
        product.Description = request.Description.Trim();
        product.CategoryId = request.CategoryId;
        product.Price = request.TransactionType == "Giveaway" ? 0 : request.Price;
        product.TransactionType = request.TransactionType;
        product.AllowRent = request.AllowRent; product.RentalPrice = request.AllowRent ? request.RentalPrice : null;
        product.Condition = request.Condition;
        product.Status = request.Status;
        product.Location = request.Location?.Trim();
        product.IsNegotiable = request.IsNegotiable && request.TransactionType == "Sell";
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.Include(p => p.Images).SingleOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        if (product.SellerId != CurrentUserId) return Forbid();
        if (product.IsHidden) return Conflict(new { error = "This listing is hidden by administration." });
        if (await db.Orders.AnyAsync(x => x.ProductId == id) ||
            await db.Conversations.AnyAsync(x => x.ProductId == id) ||
            await db.Offers.AnyAsync(x => x.ProductId == id) ||
            await db.StoreProducts.AnyAsync(x => x.ProductId == id) ||
            await db.Reports.AnyAsync(x => x.ReportedProductId == id))
            return Conflict(new { error = "This listing has activity. Mark it Unavailable instead." });
        var paths = product.Images.Select(x => ImagePath(x.ImageUrl)).ToList();
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        foreach (var path in paths) if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return NoContent();
    }

    [HttpPost("{id:int}/images")]
    [Authorize(Roles = "Student,Business Seller")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public async Task<IActionResult> AddImage(int id, [FromForm] IFormFile file)
    {
        var product = await db.Products.Include(p => p.Images).SingleOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        if (product.SellerId != CurrentUserId) return Forbid();
        if (product.IsHidden) return Conflict(new { error = "This listing is hidden by administration." });
        if (product.Images.Count >= 5) return BadRequest(new { error = "Maximum five images per product." });
        if (file is null || file.Length < 12 || file.Length > MaxImageBytes)
            return BadRequest(new { error = "Choose an image under 5 MB." });
        await using var input = file.OpenReadStream();
        var header = new byte[12];
        await input.ReadExactlyAsync(header);
        var extension = DetectImageType(header);
        if (extension is null) return BadRequest(new { error = "Use a JPEG, PNG, or WebP image." });
        Directory.CreateDirectory(UploadDirectory);
        var name = $"{Guid.NewGuid():N}.{extension}";
        var path = Path.Combine(UploadDirectory, name);
        await using (var output = System.IO.File.Create(path))
        {
            await output.WriteAsync(header);
            await input.CopyToAsync(output);
        }
        var image = new ProductImage { ProductId = id, ImageUrl = $"/api/products/images/{name}", IsPrimary = product.Images.Count == 0 };
        try { db.ProductImages.Add(image); await db.SaveChangesAsync(); }
        catch { System.IO.File.Delete(path); throw; }
        return Ok(new { image.Id, image.ImageUrl, image.IsPrimary });
    }

    [HttpGet("images/{name}")]
    public async Task<IActionResult> GetImage(string name)
    {
        if (!Regex.IsMatch(name, @"^[a-f0-9]{32}\.(jpg|png|webp)$")) return NotFound();
        var url = $"/api/products/images/{name}";
        if (!await db.ProductImages.AnyAsync(x => x.ImageUrl == url)) return NotFound();
        var path = Path.Combine(UploadDirectory, name);
        if (!System.IO.File.Exists(path)) return NotFound();
        var contentType = name.EndsWith(".png") ? "image/png" :
            name.EndsWith(".webp") ? "image/webp" : "image/jpeg";
        return PhysicalFile(path, contentType);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> RemoveImage(int id, int imageId)
    {
        var product = await db.Products.Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == id);
        if (product is null) return NotFound();
        if (product.SellerId != CurrentUserId) return Forbid();
        if (product.IsHidden) return Conflict(new { error = "This listing is hidden by administration." });
        var image = product.Images.SingleOrDefault(x => x.Id == imageId);
        if (image is null) return NotFound();
        db.ProductImages.Remove(image);
        if (image.IsPrimary)
        {
            var next = product.Images.Where(x => x.Id != imageId).OrderBy(x => x.Id).FirstOrDefault();
            if (next is not null) next.IsPrimary = true;
        }
        await db.SaveChangesAsync();
        var path = ImagePath(image.ImageUrl);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return NoContent();
    }

    [HttpPost("{id:int}/images/{imageId:int}/primary")]
    public async Task<IActionResult> Primary(int id, int imageId) {
        var p = await db.Products.Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound(); if (p.SellerId != CurrentUserId) return Forbid();
        if (p.IsHidden) return Conflict(new { error = "This listing is hidden by administration." });
        if (!p.Images.Any(x => x.Id == imageId)) return NotFound();
        foreach (var image in p.Images) image.IsPrimary = image.Id == imageId;
        await db.SaveChangesAsync(); return Ok();
    }
    [HttpGet("{id:int}/related")]
    public async Task<IActionResult> Related(int id) {
        var p = await db.Products.FindAsync(id); if (p == null) return NotFound();
        return Ok(await db.Products.Where(x => x.Id != id && x.CategoryId == p.CategoryId && x.Status == "Available" && !x.IsHidden && x.Seller.Status == "Active" && !db.StoreProducts.Any(sp => sp.ProductId == x.Id && sp.Store.IsHidden)).OrderByDescending(x => x.CreatedAt).Take(4).Select(x => new { x.Id, x.Title, x.Price, x.AllowRent, x.RentalPrice, x.TransactionType, x.Status, x.Condition, x.Location, x.SellerId, SellerName = x.Seller.FirstName + " " + x.Seller.LastName, SellerVerified = x.Seller.IsVerified, CategoryName = x.Category.CategoryName, ImageUrl = x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault() }).ToListAsync());
    }
    private async Task<string?> Validate(ProductRequest p)
    {
        if (string.IsNullOrWhiteSpace(p.Title) || string.IsNullOrWhiteSpace(p.Description))
            return "Title and description are required.";
        if (!Types.Contains(p.TransactionType) || !Conditions.Contains(p.Condition) || !Statuses.Contains(p.Status))
            return "Choose a valid type, condition, and status.";
        if (p.AllowRent && (p.TransactionType != "Sell" || p.RentalPrice is null or <= 0 or > 9999999999.99m || decimal.Round(p.RentalPrice.Value,2) != p.RentalPrice.Value)) return "Choose a positive daily rental price with at most two decimal places for a sale with renting enabled.";
        if (p.Price > 9999999999.99m || (p.Price.HasValue && decimal.Round(p.Price.Value, 2) != p.Price.Value)) return "Price supports at most two decimal places.";
        if (string.IsNullOrWhiteSpace(p.Location) || !await db.PickupLocations.AnyAsync(x => x.LocationName == p.Location)) return "Choose a campus pickup location.";
        if (!await db.Categories.AnyAsync(x => x.Id == p.CategoryId)) return "Choose a category.";
        if ((p.TransactionType is "Sell" or "Rent") && (p.Price is null or <= 0))
            return "A positive price is required for sales and rentals.";
        if (p.TransactionType == "Exchange" && (p.Price is not null and < 0))
            return "Price cannot be negative.";
        return null;
    }

    private string ImagePath(string url) => Path.Combine(UploadDirectory, Path.GetFileName(url));
    private static string? DetectImageType(byte[] h)
    {
        if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return "jpg";
        if (h.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "png";
        if (h.AsSpan(0, 4).SequenceEqual("RIFF"u8) && h.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "webp";
        return null;
    }
}
