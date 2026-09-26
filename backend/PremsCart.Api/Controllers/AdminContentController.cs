using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;
namespace PremsCart.Api.Controllers;
public record ContentEdit([Required, MaxLength(120)] string Title, [Required, MaxLength(3000)] string Description, decimal? Price);
public record VisibilityRequest(bool Hidden, [Required, MaxLength(1000)] string Reason);
[ApiController, Authorize(Roles="Admin"), Route("api/admin")]
public class AdminContentController(PremsCartDbContext db) : ControllerBase {
    int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("wanted")]
    public async Task<IActionResult> Wanted() => Ok(await db.WantedPosts.OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Title, x.Description, x.Budget, x.UserId, x.Status, x.IsHidden }).ToListAsync());
    [HttpGet("stores")]
    public async Task<IActionResult> Stores() => Ok(await db.Stores.OrderByDescending(x => x.Id).Select(x => new { x.Id, title = x.StoreName, x.Description, x.OwnerId, x.IsHidden, products = x.StoreProducts.Count }).ToListAsync());
    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews() => Ok(await db.Reviews.OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Rating, x.Comment, x.ReviewerId, x.ReviewedUserId, x.IsHidden }).ToListAsync());
    [HttpPut("content/{kind}/{id:int}")]
    public async Task<IActionResult> Edit(string kind, int id, ContentEdit input) {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Description) || input.Price < 0 || input.Price > 9999999999m || (input.Price.HasValue && decimal.Truncate(input.Price.Value) != input.Price)) return BadRequest(new { error = "Enter valid text and a non-negative whole-Taka price." });
        switch(kind) {
            case "listings":
                var p = await db.Products.FindAsync(id); if(p == null)return NotFound();
                if (p.TransactionType != "Giveaway" && (input.Price is null or <= 0)) return BadRequest(new { error = "Sales and rentals need a positive price." });
                if (await db.Orders.AnyAsync(x=>x.ProductId==id && x.Status!="Completed" && x.Status!="Cancelled") || await db.Offers.AnyAsync(x=>x.ProductId==id && (x.Status=="Pending"||x.Status=="Countered"))) return Conflict(new {error="Finish active requests before editing this item."});
                p.Title=input.Title.Trim();p.Description=input.Description.Trim();p.Price=p.TransactionType=="Giveaway"?0:input.Price;break;
            case "wanted": var w=await db.WantedPosts.FindAsync(id);if(w==null)return NotFound();w.Title=input.Title.Trim();w.Description=input.Description.Trim();w.Budget=input.Price;break;
            case "stores":if(input.Title.Trim().Length > 100 || input.Description.Length > 1000)return BadRequest(new {error="Shop names allow 100 characters and descriptions 1,000."});var s=await db.Stores.FindAsync(id);if(s==null)return NotFound();s.StoreName=input.Title.Trim();s.Description=input.Description.Trim();break;
            default:return NotFound();
        }
        await db.SaveChangesAsync();return Ok();
    }
    [HttpPost("content/{kind}/{id:int}/visibility")]
    public async Task<IActionResult> Visibility(string kind,int id,VisibilityRequest input) {
        if(string.IsNullOrWhiteSpace(input.Reason))return BadRequest();
        int owner;
        switch(kind) {
            case "listings":var p=await db.Products.FindAsync(id);if(p==null)return NotFound();p.IsHidden=input.Hidden;
                if(!input.Hidden && p.Status=="Reserved" && !await db.Orders.AnyAsync(o=>o.ProductId==id && (o.Status=="Accepted"||o.Status=="Pickup scheduled"||o.Status=="Rented"||o.Status=="Return requested")) && !await db.StoreProducts.AnyAsync(sp=>sp.ProductId==id && sp.Quantity==0))p.Status="Available";
                owner=p.SellerId;break;
            case "wanted":var w=await db.WantedPosts.FindAsync(id);if(w==null)return NotFound();w.IsHidden=input.Hidden;owner=w.UserId;break;
            case "stores":var s=await db.Stores.FindAsync(id);if(s==null)return NotFound();s.IsHidden=input.Hidden;owner=s.OwnerId;break;
            case "reviews":var r=await db.Reviews.FindAsync(id);if(r==null)return NotFound();r.IsHidden=input.Hidden;owner=r.ReviewerId;break;
            default:return NotFound();
        }
        db.Notifications.Add(new Notification { UserId=owner,Title=input.Hidden?"Content hidden":"Content restored",Message=$"{kind} #{id}: {input.Reason}",Link="/dashboard", Type="moderation" });
        db.Reports.Add(new Report { ReporterId=Me,ReportedUserId=owner,Reason=$"Admin {(input.Hidden?"hide":"restore")}: {kind} #{id}",Status="Resolved",ModeratorId=Me,ResolutionAction=input.Hidden?"Hide content":"Restore content",ResolutionNote=input.Reason });
        await db.SaveChangesAsync();return Ok();
    }
}
