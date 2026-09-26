using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PremsCart.Api.Data;
using PremsCart.Api.Models;
using PremsCart.Api.SignalR;

namespace PremsCart.Api.Controllers;

public sealed record StartConversationRequest(int ProductId);

[ApiController]
[Authorize]
[Route("api/chat")]
public sealed class ChatController(PremsCartDbContext db, IHubContext<ChatHub> hub) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("conversations")]
    public async Task<IActionResult> Start(StartConversationRequest request)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == request.ProductId && p.Status == "Available" && !p.IsHidden && p.Seller.Status == "Active" && !db.StoreProducts.Any(sp => sp.ProductId == p.Id && sp.Store.IsHidden))
            .Select(p => new { p.Id, p.SellerId }).SingleOrDefaultAsync();
        if (product is null) return NotFound(new { error = "Product is unavailable." });
        if (product.SellerId == CurrentUserId)
            return BadRequest(new { error = "You cannot message yourself about your own item." });
        var existing = await db.Conversations.AsNoTracking().SingleOrDefaultAsync(
            x => x.ProductId == product.Id && x.BuyerId == CurrentUserId && x.SellerId == product.SellerId);
        if (existing is not null) return Ok(new { existing.Id });
        var conversation = new Conversation
        {
            ProductId = product.Id, BuyerId = CurrentUserId, SellerId = product.SellerId
        };
        db.Conversations.Add(conversation);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            var duplicate = await db.Conversations.AsNoTracking().SingleAsync(
                x => x.ProductId == product.Id && x.BuyerId == CurrentUserId && x.SellerId == product.SellerId);
            return Ok(new { duplicate.Id });
        }
        await hub.Clients.User(product.SellerId.ToString()).SendAsync("InboxChanged", conversation.Id);
        return Created($"/api/chat/conversations/{conversation.Id}/messages", new { conversation.Id });
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations()
    {
        var me = CurrentUserId;
        var items = await db.Conversations.AsNoTracking()
            .Where(x => x.BuyerId == me || x.SellerId == me)
            .OrderByDescending(x => x.Messages.Max(m => (DateTime?)m.SentAt) ?? x.CreatedAt)
            .Select(x => new
            {
                x.Id, x.ProductId, ProductTitle = x.Product.Title,
                ProductPrice = x.Product.Price,
                ProductRentalPrice = x.Product.RentalPrice,
                ProductTransactionType = x.Product.TransactionType,
                ProductAllowRent = x.Product.AllowRent,
                ProductStatus = x.Product.Status,
                ProductImageUrl = x.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                    .Select(i => i.ImageUrl).FirstOrDefault(),
                OtherUserId = x.BuyerId == me ? x.SellerId : x.BuyerId,
                OtherUserName = x.BuyerId == me
                    ? x.Seller.FirstName + " " + x.Seller.LastName
                    : x.Buyer.FirstName + " " + x.Buyer.LastName,
                OtherUserImage = x.BuyerId == me ? x.Seller.ProfileImage : x.Buyer.ProfileImage,
                OfferAmount = db.Offers.Where(o => o.ProductId == x.ProductId && o.BuyerId == x.BuyerId && o.SellerId == x.SellerId)
                    .OrderByDescending(o => o.Id).Select(o => (decimal?)o.OfferAmount).FirstOrDefault(),
                OfferStatus = db.Offers.Where(o => o.ProductId == x.ProductId && o.BuyerId == x.BuyerId && o.SellerId == x.SellerId)
                    .OrderByDescending(o => o.Id).Select(o => o.Status).FirstOrDefault(),
                LastMessage = x.Messages.OrderByDescending(m => m.Id)
                    .Select(m => m.IsHidden ? "[Removed by moderation]" : m.MessageText).FirstOrDefault(),
                LastMessageAt = x.Messages.OrderByDescending(m => m.Id)
                    .Select(m => (DateTime?)m.SentAt).FirstOrDefault() ?? x.CreatedAt,
                UnreadCount = x.Messages.Count(m => m.SenderId != me && !m.IsRead)
            }).ToListAsync();
        return Ok(items);
    }

    [HttpGet("conversations/{id:int}/messages")]
    public async Task<IActionResult> History(int id, [FromQuery] int? beforeId)
    {
        var me = CurrentUserId;
        if (!await db.Conversations.AnyAsync(x => x.Id == id && (x.BuyerId == me || x.SellerId == me)))
            return NotFound();
        var query = db.Messages.AsNoTracking().Where(x => x.ConversationId == id);
        if (beforeId.HasValue) query = query.Where(x => x.Id < beforeId);
        var messages = await query.OrderByDescending(x => x.Id).Take(50)
            .Select(x => new { x.Id, x.ConversationId, x.SenderId, MessageText = x.IsHidden ? "[Removed by moderation]" : x.MessageText, x.IsRead, x.SentAt })
            .ToListAsync();
        messages.Reverse();
        var hasMore = messages.Count == 50 && await db.Messages.AnyAsync(
            x => x.ConversationId == id && x.Id < messages[0].Id);
        if (!beforeId.HasValue)
        {
            await db.Messages.Where(x => x.ConversationId == id && x.SenderId != me && !x.IsRead)
                .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.IsRead, true));
        }
        return Ok(new { items = messages, hasMore });
    }

    [HttpPost("conversations/{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var me = CurrentUserId;
        if (!await db.Conversations.AnyAsync(x => x.Id == id && (x.BuyerId == me || x.SellerId == me)))
            return NotFound();
        await db.Messages.Where(x => x.ConversationId == id && x.SenderId != me && !x.IsRead)
            .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.IsRead, true));
        return NoContent();
    }
}
