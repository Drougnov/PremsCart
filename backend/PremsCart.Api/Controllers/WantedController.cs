using Microsoft.AspNetCore.SignalR;
using PremsCart.Api.SignalR;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

public sealed record WantedRequest(
    [Required, StringLength(120)] string Title,
    [Required, StringLength(3000)] string Description,
    decimal? Budget,
    int? CategoryId,
    [Required] string Status);

[ApiController]
[Authorize]
[Route("api/wanted")]
public sealed class WantedController(PremsCartDbContext db, IHubContext<ChatHub> hub) : ControllerBase
{
    private static readonly string[] Statuses = ["Open", "Fulfilled", "Closed"];
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Browse(
        [FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] int page = 1)
    {
        if (page < 1 || page > 10000 || search?.Length > 100)
            return BadRequest(new { error = "Invalid search." });
        var query = db.WantedPosts.AsNoTracking().Where(x => x.Status == "Open" && !x.IsHidden);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Title, $"%{term}%") ||
                                     EF.Functions.ILike(x.Description, $"%{term}%"));
        }
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 12).Take(12)
            .Select(x => new
            {
                x.Id, x.Title, x.Description, x.Budget, x.Status, x.CreatedAt,
                x.CategoryId, CategoryName = x.Category == null ? null : x.Category.CategoryName,
                x.UserId, StudentName = x.User.FirstName + " " + x.User.LastName
            }).ToListAsync();
        return Ok(new { items, total, page, pageSize = 12 });
    }

    public sealed record RespondRequest(int ProductId);
    [HttpPost("{id:int}/respond"), Authorize(Roles="Student,Business Seller")]
    public async Task<IActionResult> Respond(int id, RespondRequest input) {
        var wanted=await db.WantedPosts.Include(w=>w.User).SingleOrDefaultAsync(w=>w.Id==id);
        if(wanted==null||wanted.IsHidden||wanted.Status!="Open"||wanted.User.Status!="Active"||!wanted.User.IsVerified)return NotFound(new {error="This wanted post is no longer open."});
        if(wanted.UserId==CurrentUserId)return BadRequest(new {error="You cannot respond to your own wanted post."});
        var product=await db.Products.SingleOrDefaultAsync(p=>p.Id==input.ProductId && p.SellerId==CurrentUserId && !p.IsHidden && p.Status=="Available" && !db.StoreProducts.Any(sp=>sp.ProductId==p.Id && (sp.Store.IsHidden||sp.Quantity<1)));
        if(product==null)return BadRequest(new {error="Choose one of your available items."});
        var conversation=await db.Conversations.SingleOrDefaultAsync(c=>c.ProductId==product.Id&&c.BuyerId==wanted.UserId&&c.SellerId==CurrentUserId);
        if(conversation==null){
            conversation=new Conversation{ProductId=product.Id,BuyerId=wanted.UserId,SellerId=CurrentUserId};db.Conversations.Add(conversation);
            try{await db.SaveChangesAsync();}catch(DbUpdateException ex) when(ex.InnerException is Npgsql.PostgresException{SqlState:"23505"}){db.Entry(conversation).State=EntityState.Detached;conversation=await db.Conversations.SingleAsync(c=>c.ProductId==product.Id&&c.BuyerId==wanted.UserId&&c.SellerId==CurrentUserId);}
        }
        var text=$"In response to request #{wanted.Id}: {wanted.Title}. I have {product.Title} available. Use the linked item card and choose View item to buy it, request a rental, or request it as a giveaway.";
        if(!await db.Messages.AnyAsync(m=>m.ConversationId==conversation.Id&&m.SenderId==CurrentUserId&&m.MessageText==text)){
            var message=new Message{ConversationId=conversation.Id,SenderId=CurrentUserId,MessageText=text};db.Messages.Add(message);await db.SaveChangesAsync();
            await hub.Clients.Group($"conversation:{conversation.Id}").SendAsync("ReceiveMessage",new{message.Id,message.ConversationId,message.SenderId,message.MessageText,message.IsRead,message.SentAt});
        }
        await hub.Clients.Users(wanted.UserId.ToString(),CurrentUserId.ToString()).SendAsync("InboxChanged",conversation.Id);
        return Ok(new{conversationId=conversation.Id});
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine() =>
        Ok(await db.WantedPosts.AsNoTracking().Where(x => x.UserId == CurrentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id, x.Title, x.Description, x.Budget, x.Status, x.CreatedAt,
                x.CategoryId, CategoryName = x.Category == null ? null : x.Category.CategoryName
            }).ToListAsync());

    [HttpPost]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> Create(WantedRequest request)
    {
        var error = await Validate(request);
        if (error is not null) return BadRequest(new { error });
        var post = new WantedPost
        {
            UserId = CurrentUserId, Title = request.Title.Trim(),
            Description = request.Description.Trim(), Budget = request.Budget,
            CategoryId = request.CategoryId, Status = request.Status
        };
        db.WantedPosts.Add(post);
        await db.SaveChangesAsync();
        return Created($"/api/wanted/{post.Id}", new { post.Id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var post = await db.WantedPosts.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.IsHidden, x.UserId, x.Title, x.Description, x.Budget, x.Status,
                x.CategoryId, CategoryName = x.Category == null ? null : x.Category.CategoryName,
                StudentName = x.User.FirstName + " " + x.User.LastName, x.CreatedAt
            }).SingleOrDefaultAsync();
        return post is null || (post.IsHidden && post.UserId != CurrentUserId) || (post.Status != "Open" && post.UserId != CurrentUserId)
            ? NotFound() : Ok(post);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> Update(int id, WantedRequest request)
    {
        var post = await db.WantedPosts.SingleOrDefaultAsync(x => x.Id == id);
        if (post is null) return NotFound();
        if (post.UserId != CurrentUserId) return Forbid();
        if (post.IsHidden) return Conflict(new { error = "This post is hidden by administration." });
        var error = await Validate(request);
        if (error is not null) return BadRequest(new { error });
        post.Title = request.Title.Trim();
        post.Description = request.Description.Trim();
        post.Budget = request.Budget;
        post.CategoryId = request.CategoryId;
        post.Status = request.Status;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Student,Business Seller")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await db.WantedPosts.SingleOrDefaultAsync(x => x.Id == id);
        if (post is null) return NotFound();
        if (post.UserId != CurrentUserId) return Forbid();
        if (post.IsHidden) return Conflict(new { error = "This post is hidden by administration." });
        db.WantedPosts.Remove(post);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> Validate(WantedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return "Title and description are required.";
        if (!Statuses.Contains(request.Status)) return "Choose a valid status.";
        if (request.Budget is < 0 or > 9999999999m || (request.Budget.HasValue && decimal.Truncate(request.Budget.Value) != request.Budget.Value)) return "Budget must be a whole-Taka amount from 0 to 9,999,999,999.";
        if (request.CategoryId.HasValue && !await db.Categories.AnyAsync(x => x.Id == request.CategoryId))
            return "Choose a valid category.";
        return null;
    }
}
