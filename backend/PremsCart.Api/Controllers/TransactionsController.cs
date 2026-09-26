using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

public sealed record MakeOfferRequest(int ProductId, decimal Amount);
public sealed record CounterOfferRequest(decimal Amount);
public sealed record CreateOrderRequest(int ProductId, int? RentalDays = null, DateOnly? RentalStartDate = null, string? Mode = null);
public sealed record SchedulePickupRequest(int LocationId, DateTime PickupTime);

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionsController(PremsCartDbContext db) : ControllerBase
{
    private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly string[] ActiveOrders = ["Pending", "Accepted", "Pickup scheduled", "Rented", "Return requested"];

    [HttpGet("locations")]
    public async Task<IActionResult> Locations() => Ok(await db.PickupLocations.AsNoTracking()
        .OrderBy(x => x.LocationName).Select(x => new { x.Id, x.LocationName }).ToListAsync());

    [HttpGet("offers")]
    public async Task<IActionResult> Offers() => Ok(await db.Offers.AsNoTracking()
        .Where(x => x.BuyerId == Me || x.SellerId == Me).OrderByDescending(x => x.CreatedAt)
        .Select(x => new { x.Id, x.ProductId, ProductTitle = x.Product.Title, x.BuyerId,
            BuyerName = x.Buyer.FirstName + " " + x.Buyer.LastName, x.SellerId,
            SellerName = x.Seller.FirstName + " " + x.Seller.LastName,
            x.LastProposerId, History = x.Proposals.OrderBy(p => p.Id).Select(p => new { p.AuthorId, p.Amount, p.CreatedAt }).ToList(), x.OfferAmount, x.Status, x.CreatedAt }).ToListAsync());

    [Authorize(Roles = "Student,Business Seller"), HttpPost("offers")]
    public async Task<IActionResult> MakeOffer(MakeOfferRequest input)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == input.ProductId && !x.IsHidden && x.Seller.Status == "Active" && x.Seller.IsVerified && !db.StoreProducts.Any(sp => sp.ProductId == x.Id && (sp.Store.IsHidden || sp.Quantity < 1)) && (x.TransactionType == "Sell" || x.TransactionType == "Giveaway" || x.TransactionType == "Rent"));
        if (product is null || product.Status != "Available") return NotFound(new { error = "Item unavailable." });
        if (product.SellerId == Me) return BadRequest(new { error = "You cannot offer on your own item." });
        if (product.TransactionType != "Sell")
            return BadRequest(new { error = "Use the checkout form for giveaways and rentals. Rentals use the listed daily rate." });
        if (input.Amount <= 0 || input.Amount > 9999999999m || decimal.Truncate(input.Amount) != input.Amount)
            return BadRequest(new { error = "Enter a whole-Taka amount greater than zero." });
        if (!product.IsNegotiable && input.Amount != product.Price)
            return BadRequest(new { error = "This item accepts its asking price only." });
        if (await db.Offers.AnyAsync(x => x.ProductId == product.Id && x.BuyerId == Me &&
                (x.Status == "Pending" || x.Status == "Countered")) ||
            await db.Orders.AnyAsync(x => x.ProductId == product.Id && x.BuyerId == Me && ActiveOrders.Contains(x.Status)))
            return Conflict(new { error = "You already have an active offer or order for this item." });
        var offer = new Offer { ProductId = product.Id, BuyerId = Me, SellerId = product.SellerId, OfferAmount = input.Amount, LastProposerId = Me, Proposals = [new OfferProposal { AuthorId = Me, Amount = input.Amount }] };
        db.Offers.Add(offer);
        await db.SaveChangesAsync();
        return Created($"/api/transactions/offers/{offer.Id}", new { offer.Id });
    }

    [HttpPost("offers/{id:int}/counter")]
    public async Task<IActionResult> Counter(int id, CounterOfferRequest input)
    {
        var offer = await db.Offers.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);
        if (offer is null) return NotFound();
        if ((offer.SellerId != Me && offer.BuyerId != Me) || (offer.LastProposerId ?? offer.BuyerId) == Me) return Forbid();
        if (offer.Status is not ("Pending" or "Countered") || (offer.Product.Status != "Available" || offer.Product.IsHidden)) return Conflict(new { error = "Offer is no longer open." });
        if (!offer.Product.IsNegotiable) return BadRequest(new { error = "This item has a fixed price." });
        if (input.Amount <= 0 || input.Amount > 9999999999m || decimal.Truncate(input.Amount) != input.Amount || input.Amount == offer.OfferAmount)
            return BadRequest(new { error = "Enter a different whole-Taka amount greater than zero." });
        offer.LastProposerId = Me;
        db.OfferProposals.Add(new OfferProposal { OfferId = offer.Id, AuthorId = Me, Amount = input.Amount });
        offer.OfferAmount = input.Amount;
        offer.Status = "Countered";
        await db.SaveChangesAsync();
        return Ok(new { offer.Id, offer.OfferAmount, offer.Status });
    }

    [HttpPost("offers/{id:int}/reject")]
    public async Task<IActionResult> RejectOffer(int id)
    {
        var offer = await db.Offers.SingleOrDefaultAsync(x => x.Id == id);
        if (offer is null) return NotFound();
        if ((offer.SellerId != Me && offer.BuyerId != Me) || (offer.LastProposerId ?? offer.BuyerId) == Me) return Forbid();
        if (offer.Status is not ("Pending" or "Countered")) return Conflict(new { error = "Offer is closed." });
        offer.Status = "Rejected";
        await db.SaveChangesAsync();
        return Ok(new { offer.Id, offer.Status });
    }

    [HttpPost("offers/{id:int}/accept")]
    public async Task<IActionResult> AcceptOffer(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var offer = await db.Offers.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);
        if (offer is null) return NotFound();
        if ((offer.SellerId != Me && offer.BuyerId != Me) || (offer.LastProposerId ?? offer.BuyerId) == Me) return Forbid();
        if (offer.Status is not ("Pending" or "Countered") || (offer.Product.Status != "Available" || offer.Product.IsHidden))
            return Conflict(new { error = "Offer or item is no longer available." });
        if (await db.Orders.AnyAsync(x => x.ProductId == offer.ProductId && x.BuyerId == offer.BuyerId && ActiveOrders.Contains(x.Status)))
            return Conflict(new { error = "An active order already exists." });
        if (offer.Product.TransactionType != "Sell") return Conflict(new { error = "Only sales support negotiated offers. Use a new rental request with a duration." });
        if (await db.StoreProducts.AnyAsync(x => x.ProductId == offer.ProductId && x.Store.IsHidden) || await db.Users.AnyAsync(x => (x.Id == offer.BuyerId || x.Id == offer.SellerId) && x.Status != "Active")) return Conflict(new { error = "This account or shop is unavailable." });
        var stock = offer.Product.TransactionType == "Rent" ? null : await db.StoreProducts
            .SingleOrDefaultAsync(x => x.ProductId == offer.ProductId);
        if (stock is not null && stock.Quantity < 1) return Conflict(new { error = "Item is out of stock." });
        offer.Status = "Accepted";
        var order = new Order { ProductId = offer.ProductId, BuyerId = offer.BuyerId,
            SellerId = offer.SellerId, FinalPrice = offer.OfferAmount, Status = "Accepted" };
        db.Orders.Add(order);
        if (offer.Product.TransactionType != "Rent")
        {
            if (stock is not null) stock.Quantity--;
            if (stock is null || stock.Quantity == 0)
            {
                offer.Product.Status = "Reserved";
                await CloseOtherOffers(offer.ProductId, offer.Id);
            }
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { offer.Id, offer.Status, orderId = order.Id });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders() => Ok(await db.Orders.AsNoTracking()
        .Where(x => x.BuyerId == Me || x.SellerId == Me).OrderByDescending(x => x.CreatedAt)
        .Select(x => new { x.Id, x.ProductId, ProductTitle = x.Product.Title,
            TransactionType = x.RentalDays.HasValue ? "Rent" : x.Product.TransactionType, HasReviewed = db.Reviews.Any(r=>r.OrderId==x.Id && r.ReviewerId==Me), x.BuyerId, BuyerName = x.Buyer.FirstName + " " + x.Buyer.LastName,
            x.SellerId, SellerName = x.Seller.FirstName + " " + x.Seller.LastName,
            x.RentalStartDate, x.RentalDays, x.RentalStartedAt, x.RentalDueAt, x.ReturnedAt, x.FinalPrice, x.PickupLocationId, PickupLocation = x.PickupLocation != null ? x.PickupLocation.LocationName : null,
            x.PickupTime, x.PickupStatus, x.PickupProposerId, x.CompletedAt, x.Status, x.CreatedAt }).ToListAsync());

    [Authorize(Roles = "Student,Business Seller"), HttpPost("orders")]
    public async Task<IActionResult> RequestOrder(CreateOrderRequest input)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == input.ProductId && !x.IsHidden && x.Seller.Status == "Active" && x.Seller.IsVerified && !db.StoreProducts.Any(sp => sp.ProductId == x.Id && (sp.Store.IsHidden || sp.Quantity < 1)) && (x.TransactionType == "Sell" || x.TransactionType == "Giveaway" || x.TransactionType == "Rent"));
        if (product is null || product.Status != "Available") return NotFound(new { error = "Item unavailable." });
        if (product.SellerId == Me) return BadRequest(new { error = "You cannot request your own item." });
        if (await db.Orders.AnyAsync(x => x.ProductId == product.Id && x.BuyerId == Me && ActiveOrders.Contains(x.Status)) ||
            await db.Offers.AnyAsync(x => x.ProductId == product.Id && x.BuyerId == Me &&
                (x.Status == "Pending" || x.Status == "Countered")))
            return Conflict(new { error = "You already have an active offer or order." });
        var mode = input.Mode ?? (product.TransactionType=="Rent" || (product.AllowRent && input.RentalDays.HasValue) ? "Rent" : product.TransactionType);
        if (mode!=product.TransactionType && !(mode=="Rent" && product.AllowRent)) return BadRequest(new {error="This transaction option is not available."});
        if (mode=="Rent" && input.RentalDays is not (>=1 and <=30)) return BadRequest(new {error="Choose 1–30 rental days."});
        if (mode!="Rent" && (input.RentalDays.HasValue || input.RentalStartDate.HasValue)) return BadRequest(new {error="Rental dates and duration are only for renting."});
        if (input.RentalStartDate.HasValue && (input.RentalStartDate<DateOnly.FromDateTime(DateTime.UtcNow) || input.RentalStartDate>DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)))) return BadRequest(new {error="Choose a rental pickup within 90 days."});
        var unit = mode=="Giveaway" ? 0 : mode=="Rent" && product.AllowRent ? product.RentalPrice : product.Price;
        if(unit==null || unit<0 || unit*(input.RentalDays??1)>9999999999.99m) return BadRequest(new {error="Invalid total."});
        var order = new Order { RentalStartDate=input.RentalStartDate, RentalDays=mode=="Rent"?input.RentalDays:null, ProductId=product.Id, BuyerId=Me, SellerId=product.SellerId, FinalPrice=unit*(mode=="Rent"?input.RentalDays!.Value:1) };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Created($"/api/transactions/orders/{order.Id}", new { order.Id });
    }

    [HttpPost("orders/{id:int}/accept")]
    public async Task<IActionResult> AcceptOrder(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order = await db.Orders.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (order.SellerId != Me) return Forbid();
        if (order.Status != "Pending" || (order.Product.Status != "Available" || order.Product.IsHidden)) return Conflict(new { error = "Order or item is unavailable." });
        if (await db.StoreProducts.AnyAsync(x => x.ProductId == order.ProductId && x.Store.IsHidden) || await db.Users.AnyAsync(x => x.Id == order.BuyerId && x.Status != "Active")) return Conflict(new { error = "This account or shop is unavailable." });
        var stock = order.RentalDays.HasValue ? null : await db.StoreProducts
            .SingleOrDefaultAsync(x => x.ProductId == order.ProductId);
        if (stock is not null && stock.Quantity < 1) return Conflict(new { error = "Item is out of stock." });
        if ((order.Product.TransactionType=="Rent" || order.RentalDays.HasValue) && (order.RentalDays is null or < 1 or > 30)) return Conflict(new { error = "Legacy rental request needs to be cancelled and recreated with a duration." });
        order.Status = "Accepted";
        if (order.RentalDays.HasValue) {
            order.Product.Status = "Reserved";
            await CloseOtherOffers(order.ProductId, exceptOrderId: order.Id);
        }
        if (!order.RentalDays.HasValue)
        {
            if (stock is not null) stock.Quantity--;
            if (stock is null || stock.Quantity == 0)
            {
                order.Product.Status = "Reserved";
                await CloseOtherOffers(order.ProductId, exceptOrderId: order.Id);
            }
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { order.Id, order.Status });
    }

    [HttpPost("orders/{id:int}/pickup")]
    public async Task<IActionResult> Schedule(int id, SchedulePickupRequest input)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (order.SellerId != Me && order.BuyerId != Me) return Forbid();
        if (order.Status is not ("Accepted" or "Pickup scheduled")) return Conflict(new { error = "Accept the order first." });
        if (input.PickupTime.Kind != DateTimeKind.Utc || input.PickupTime <= DateTime.UtcNow || input.PickupTime > DateTime.UtcNow.AddDays(90))
            return BadRequest(new { error = "Choose a future pickup time within 90 days." });
        if (!await db.PickupLocations.AnyAsync(x => x.Id == input.LocationId))
            return BadRequest(new { error = "Choose a campus pickup location." });
        order.PickupLocationId = input.LocationId;
        order.PickupTime = input.PickupTime;
        order.Status = "Pickup scheduled";
        order.PickupStatus = "Proposed";
        order.PickupProposerId = Me;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { order.Id, order.Status });
    }

    [HttpPost("orders/{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order = await db.Orders.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (order.BuyerId != Me) return Forbid();
        if (order.Status != "Pickup scheduled" || order.PickupStatus != "Confirmed" || order.PickupTime > DateTime.UtcNow)
            return Conflict(new { error = "Confirm after the scheduled campus pickup." });
        if (order.RentalDays.HasValue) {
            if (order.RentalDays is null) return Conflict();
            order.Status = "Rented"; order.PickupStatus = "Completed";
            order.RentalStartedAt = DateTime.UtcNow; order.RentalDueAt = DateTime.UtcNow.AddDays(order.RentalDays.Value);
            await db.SaveChangesAsync(); await transaction.CommitAsync(); return Ok(new { order.Id, order.Status, order.RentalDueAt });
        }
        order.Status = "Completed";
        order.CompletedAt = DateTime.UtcNow;
        order.PickupStatus = "Completed";
        if (!order.RentalDays.HasValue &&
            !await db.StoreProducts.AnyAsync(x => x.ProductId == order.ProductId && x.Quantity > 0))
            order.Product.Status = order.Product.TransactionType == "Giveaway" ? "GivenAway" : "Sold";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { order.Id, order.Status });
    }

    [HttpPost("orders/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order = await db.Orders.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (order.BuyerId != Me && order.SellerId != Me) return Forbid();
        if (order.Status is not ("Pending" or "Accepted" or "Pickup scheduled")) return Conflict(new { error = "Order is already closed." });
        var wasAccepted = order.Status != "Pending";
        order.Status = "Cancelled";
        StoreProduct? stock = null;
        if (wasAccepted && !order.RentalDays.HasValue)
        {
            stock = await db.StoreProducts.SingleOrDefaultAsync(x => x.ProductId == order.ProductId);
            if (stock is not null) stock.Quantity++;
        }
        if (wasAccepted && !order.Product.IsHidden && order.Product.Status == "Reserved" &&
            (stock is not null && stock.Quantity > 0 ||
             !await db.Orders.AnyAsync(x => x.ProductId == order.ProductId && x.Id != id &&
                 (x.Status == "Accepted" || x.Status == "Pickup scheduled" || x.Status == "Rented" || x.Status == "Return requested"))))
            order.Product.Status = "Available";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { order.Id, order.Status });
    }

    [HttpPost("offers/{id:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int id) {
        var o = await db.Offers.FindAsync(id);
        if (o == null) return NotFound();
        if (o.BuyerId != Me) return Forbid();
        if (o.Status is not ("Pending" or "Countered")) return Conflict();
        o.Status = "Withdrawn"; await db.SaveChangesAsync(); return Ok();
    }
    [HttpPost("orders/{id:int}/pickup/confirm")]
    public async Task<IActionResult> ConfirmPickup(int id) {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var o = await db.Orders.FindAsync(id);
        if (o == null) return NotFound();
        if ((o.BuyerId != Me && o.SellerId != Me) || o.PickupProposerId == Me) return Forbid();
        if (o.Status != "Pickup scheduled" || o.PickupStatus != "Proposed") return Conflict(new { error = "This pickup proposal has changed. Refresh to see the latest arrangement." });
        if (!o.PickupTime.HasValue || o.PickupTime <= DateTime.UtcNow) return Conflict(new { error = "The proposed time has passed. Suggest a new pickup time." });
        o.PickupStatus = "Confirmed"; await db.SaveChangesAsync(); await transaction.CommitAsync(); return Ok();
    }
    [HttpPost("orders/{id:int}/return")]
    public async Task<IActionResult> RequestReturn(int id) {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order = await db.Orders.FindAsync(id);
        if (order == null) return NotFound(); if (order.BuyerId != Me) return Forbid();
        if (order.Status != "Rented") return Conflict(new { error = "This rental is not active." });
        order.Status = "Return requested"; await db.SaveChangesAsync(); await transaction.CommitAsync(); return Ok();
    }
    [HttpPost("orders/{id:int}/return/confirm")]
    public async Task<IActionResult> ConfirmReturn(int id) {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var order = await db.Orders.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound(); if (order.SellerId != Me) return Forbid();
        if (order.Status != "Return requested") return Conflict(new { error = "The renter must request a return first." });
        order.Status = "Completed"; order.ReturnedAt = DateTime.UtcNow; order.CompletedAt = DateTime.UtcNow;
        if (!order.Product.IsHidden && order.Product.Status == "Reserved") order.Product.Status = "Available";
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
    }
    [HttpPost("orders/{id:int}/conversation")]
    public async Task<IActionResult> ConversationForOrder(int id) {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && (o.BuyerId == Me || o.SellerId == Me));
        if(order == null) return NotFound();
        var conversation = await db.Conversations.SingleOrDefaultAsync(c => c.ProductId == order.ProductId && c.BuyerId == order.BuyerId && c.SellerId == order.SellerId);
        if(conversation != null) return Ok(new { conversation.Id });
        conversation = new Conversation { ProductId=order.ProductId, BuyerId=order.BuyerId, SellerId=order.SellerId };
        db.Conversations.Add(conversation);
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateException ex) when(ex.InnerException is Npgsql.PostgresException { SqlState: "23505" }) {
            var existing=await db.Conversations.AsNoTracking().SingleAsync(c=>c.ProductId==order.ProductId&&c.BuyerId==order.BuyerId&&c.SellerId==order.SellerId);
            return Ok(new {existing.Id});
        }
        return Ok(new { conversation.Id });
    }
    private async Task CloseOtherOffers(int productId, int? exceptId = null, int? exceptOrderId = null) {
        foreach (var o in await db.Offers.Where(x => x.ProductId == productId && x.Id != exceptId && (x.Status == "Pending" || x.Status == "Countered")).ToListAsync()) o.Status = "Rejected";
        foreach (var o in await db.Orders.Where(x => x.ProductId == productId && x.Id != exceptOrderId && x.Status == "Pending").ToListAsync()) o.Status = "Cancelled";
    }
}
