using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

public sealed record AddReviewRequest(int OrderId, int Rating, string? Comment);
public sealed record FileReportRequest(int? ProductId, int? UserId, string Reason, int? ReviewId = null, int? MessageId = null);
public sealed record ResolveReportRequest(string Decision, string? Note = null);

[ApiController]
[Authorize]
[Route("api/community")]
public sealed class CommunityController(PremsCartDbContext db) : ControllerBase
{
    private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("reputation/{userId:int}")]
    public async Task<IActionResult> Reputation(int userId)
    {
        var user = await db.Users.AsNoTracking().Where(u => u.Id == userId && u.IsVerified)
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName, u.IsVerified })
            .SingleOrDefaultAsync();
        if (user is null) return NotFound();
        var reviews = await db.Reviews.AsNoTracking().Where(r => r.ReviewedUserId == userId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt).Take(30)
            .Select(r => new { r.Id, r.Rating, r.Comment, r.CreatedAt,
                ReviewerName = r.Reviewer.FirstName + " " + r.Reviewer.LastName,
                ProductTitle = r.Order.Product.Title }).ToListAsync();
        var stats = await db.Reviews.AsNoTracking().Where(r => r.ReviewedUserId == userId && !r.IsHidden)
            .GroupBy(r => r.ReviewedUserId)
            .Select(g => new { Count = g.Count(), Average = g.Average(x => (double)x.Rating) })
            .SingleOrDefaultAsync();
        return Ok(new { user.Id, user.Name, user.IsVerified, ReviewCount = stats?.Count ?? 0,
            AverageRating = stats?.Average, Reviews = reviews });
    }

    [HttpGet("reviews/mine")]
    public async Task<IActionResult> MyReviews() => Ok(await db.Reviews.Where(x => (x.ReviewerId == Me || x.ReviewedUserId == Me) && !x.IsHidden).OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Rating, x.Comment, x.ReviewerId, x.ReviewedUserId, x.OrderId }).ToListAsync());
    [HttpPost("reviews")]
    public async Task<IActionResult> Review(AddReviewRequest input)
    {
        if (input.Rating < 1 || input.Rating > 5 || input.Comment?.Length > 1000)
            return BadRequest(new { error = "Choose 1–5 stars and keep the comment under 1,000 characters." });
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.OrderId);
        if (order is null) return NotFound(new { error = "Order not found." });
        if (order.BuyerId != Me && order.SellerId != Me) return Forbid();
        if (order.Status != "Completed") return Conflict(new { error = "Complete the order before reviewing." });
        var reviewedId = Me == order.BuyerId ? order.SellerId : order.BuyerId;
        if (await db.Reviews.AnyAsync(x => x.OrderId == order.Id && x.ReviewerId == Me))
            return Conflict(new { error = "You have already reviewed this order." });
        var review = new Review { OrderId = order.Id, ReviewerId = Me, ReviewedUserId = reviewedId,
            Rating = input.Rating, Comment = input.Comment?.Trim() };
        db.Reviews.Add(review);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { return Conflict(new { error = "You have already reviewed this order." }); }
        return Created($"/api/community/reputation/{reviewedId}", new { review.Id });
    }

    [HttpPost("reports")]
    public async Task<IActionResult> Report(FileReportRequest input)
    {
        if ((new int?[] { input.ProductId, input.UserId, input.ReviewId, input.MessageId }.Count(x => x.HasValue) != 1) ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 1000)
            return BadRequest(new { error = "Choose one item or user and give a reason up to 1,000 characters." });
        if (input.ProductId.HasValue && !await db.Products.AnyAsync(p => p.Id == input.ProductId))
            return NotFound(new { error = "Item not found." });
        if (input.UserId.HasValue && !await db.Users.AnyAsync(u => u.Id == input.UserId))
            return NotFound(new { error = "User not found." });
        if (input.UserId == Me || input.ProductId.HasValue &&
            await db.Products.AnyAsync(p => p.Id == input.ProductId && p.SellerId == Me))
            return BadRequest(new { error = "You cannot report yourself or your own item." });
        if (input.ReviewId.HasValue && !await db.Reviews.AnyAsync(x => x.Id == input.ReviewId && !x.IsHidden)) return NotFound();
        if (input.MessageId.HasValue && !await db.Messages.AnyAsync(x => x.Id == input.MessageId && (x.Conversation.BuyerId == Me || x.Conversation.SellerId == Me))) return NotFound();
        var report = new Report { ReviewId = input.ReviewId, MessageId = input.MessageId, ReporterId = Me, ReportedProductId = input.ProductId,
            ReportedUserId = input.UserId, Reason = input.Reason.Trim() };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return Created($"/api/community/reports/{report.Id}", new { report.Id });
    }

    [HttpGet("reports/mine")]
    public async Task<IActionResult> MyReports() => Ok(await db.Reports.AsNoTracking()
        .Where(r => r.ReporterId == Me).OrderByDescending(r => r.CreatedAt)
        .Select(r => new { r.Id, r.ReportedProductId, r.ReportedUserId, r.Reason, r.Status, r.CreatedAt })
        .ToListAsync());

    [HttpGet("reports/moderation")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Queue() => Ok(await db.Reports.AsNoTracking()
        .Where(r => r.Status == "Pending").OrderBy(r => r.CreatedAt).Take(100)
        .Select(r => new { r.Id, r.ReporterId,
            ReporterName = r.Reporter.FirstName + " " + r.Reporter.LastName,
            r.ReportedProductId, ProductTitle = r.ReportedProduct != null ? r.ReportedProduct.Title : null,
            r.ReportedUserId, UserName = r.ReportedUser != null ? r.ReportedUser.FirstName + " " + r.ReportedUser.LastName : null,
            r.Reason, r.Status, r.CreatedAt }).ToListAsync());

    [HttpPost("reports/{id:int}/resolve")]
    [Authorize(Roles = "Moderator,Admin")]
    public async Task<IActionResult> Resolve(int id, ResolveReportRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Note) || input.Note.Length > 1000) return BadRequest(new { error = "Enter a resolution reason up to 1,000 characters." });
        if (input.Decision is not ("Dismiss" or "Resolve" or "Hide listing" or "Hide content"))
            return BadRequest(new { error = "Choose Dismiss, Resolve, or Hide item." });
        var report = await db.Reports.SingleOrDefaultAsync(r => r.Id == id);
        if (report is null) return NotFound();
        if (report.Status != "Pending") return Conflict(new { error = "Report was already reviewed." });
        if (input.Decision == "Hide listing")
        {
            if (report.ReportedProductId is not int productId)
                return BadRequest(new { error = "This report is about a user, not an item." });
            var product = await db.Products.FindAsync(productId);
            if (product is null) return NotFound();
            product.IsHidden = true;
            db.Notifications.Add(new Notification { UserId = product.SellerId, Title = "Item hidden", Message = input.Note ?? report.Reason, Link = "/dashboard/listings", Type = "moderation" });
        }
        if (input.Decision == "Hide content") {
            if (report.ReviewId is int rid) { var r = await db.Reviews.FindAsync(rid); if (r != null) r.IsHidden = true; }
            else if (report.MessageId is int mid) { var m = await db.Messages.FindAsync(mid); if (m != null) m.IsHidden = true; }
            else return BadRequest(new { error = "This report is not about a review or message." });
        }
        report.ResolutionAction = input.Decision; report.ResolutionNote = input.Note?.Trim(); report.ModeratorId = Me;
        db.Notifications.Add(new Notification { UserId = report.ReporterId, Title = "Report reviewed", Message = input.Decision + ": " + input.Note, Link = "/dashboard/reviews", Type = "reports" });
        report.Status = input.Decision == "Dismiss" ? "Dismissed" : "Resolved";
        await db.SaveChangesAsync();
        return Ok(new { report.Id, report.Status });
    }
}
