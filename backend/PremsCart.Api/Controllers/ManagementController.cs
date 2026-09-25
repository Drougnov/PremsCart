using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;
namespace PremsCart.Api.Controllers;
public record UserActionRequest([Required] string Action, [Required, MaxLength(1000)] string Reason);
public record LookupRequest([Required, MaxLength(120)] string Name, [MaxLength(20)] string? Code);
[ApiController, Authorize(Roles = "Admin,Moderator")]
public class ManagementController(PremsCartDbContext db) : ControllerBase {
 int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet("api/moderator/users")]
 public async Task<IActionResult> Users() => Ok(await db.Users.OrderBy(x => x.Id).Select(x => new { x.Id, x.FirstName, x.LastName, x.IsVerified, x.UniversityEmail, x.Status, x.SuspensionReason, role = x.Role.RoleName }).ToListAsync());
 [HttpPost("api/moderator/users/{id:int}/action")]
 public async Task<IActionResult> Act(int id, UserActionRequest input) {
    if (string.IsNullOrWhiteSpace(input.Reason) || input.Action is not ("Warn" or "Suspend" or "Reactivate")) return BadRequest(new { error = "Choose an action and enter a reason." });
    var u = await db.Users.FindAsync(id); if (u == null) return NotFound();
    if (id == Me || u.RoleId == 4 || (u.RoleId == 3 && !User.IsInRole("Admin"))) return Forbid();
    if (input.Action != "Warn") { u.Status = input.Action == "Suspend" ? "Suspended" : "Active"; u.SuspensionReason = input.Action == "Suspend" ? input.Reason : null; u.TokenVersion++; }
    db.Notifications.Add(new Notification { UserId = id, Title = input.Action, Message = input.Reason, Link = "/settings/profile" });
    db.Reports.Add(new Report { ReporterId = Me, ReportedUserId = id, Reason = "Moderator action: " + input.Action, ResolutionNote = input.Reason, ModeratorId = Me, Status = "Resolved" });
    await db.SaveChangesAsync(); return Ok();
 }
 [HttpGet("api/moderator/reports")]
 public async Task<IActionResult> Reports() => Ok(await db.Reports.OrderByDescending(x => x.Id).Take(300).Select(x => new { x.Id, x.ReporterId, x.ReportedProductId, x.ReportedUserId, x.ReviewId, x.MessageId, x.Reason, x.Status, x.ResolutionNote, x.ResolutionAction, x.ModeratorId,
    content = x.ReviewId != null ? db.Reviews.Where(r => r.Id == x.ReviewId).Select(r => r.Comment).FirstOrDefault() : x.MessageId != null ? db.Messages.Where(m => m.Id == x.MessageId).Select(m => m.MessageText).FirstOrDefault() : null }).ToListAsync());
 [Authorize(Roles = "Admin"), HttpGet("api/admin/dashboard")]
 public async Task<IActionResult> Stats() => Ok(new { users = await db.Users.CountAsync(), listings = await db.Products.CountAsync(), activeListings = await db.Products.CountAsync(x => x.Status == "Available" && !x.IsHidden), transactions = await db.Orders.CountAsync(), stores = await db.Stores.CountAsync(), wantedPosts = await db.WantedPosts.CountAsync(), activeRentals = await db.Orders.CountAsync(x => x.Status == "Rented" || x.Status == "Return requested"), pendingReports = await db.Reports.CountAsync(x => x.Status == "Pending"), recentActivity = await db.Reports.Where(x => x.Status != "Pending").OrderByDescending(x => x.Id).Take(6).Select(x => new { x.Id, x.Reason, x.Status, x.ResolutionAction, x.ResolutionNote }).ToListAsync() });
 [Authorize(Roles = "Admin"), HttpGet("api/admin/listings")]
 public async Task<IActionResult> Listings() => Ok(await db.Products.OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Title, x.Description, x.IsHidden, x.SellerId, x.Price, x.Status, x.TransactionType }).ToListAsync());
 [Authorize(Roles = "Admin"), HttpGet("api/admin/transactions")]
 public async Task<IActionResult> Transactions() => Ok(await db.Orders.OrderByDescending(x => x.Id).Select(x => new { x.Id, x.ProductId, x.BuyerId, x.SellerId, x.Status, x.RentalStartDate, x.RentalDays, x.RentalDueAt, x.ReturnedAt, x.FinalPrice, x.PickupStatus }).ToListAsync());
 [Authorize(Roles = "Admin"), HttpGet("api/admin/lookups/{kind}")]
 public async Task<IActionResult> Lookups(string kind) => kind switch {
    "categories" => Ok(await db.Categories.Select(x => new { x.Id, name = x.CategoryName, code = "" }).ToListAsync()),
    "departments" => Ok(await db.Departments.Select(x => new { x.Id, name = x.DepartmentName, code = x.Code }).ToListAsync()),
    "pickup-locations" => Ok(await db.PickupLocations.Select(x => new { x.Id, name = x.LocationName, code = "" }).ToListAsync()), _ => NotFound()
 };
 [Authorize(Roles = "Admin"), HttpPost("api/admin/lookups/{kind}"), HttpPut("api/admin/lookups/{kind}/{id:int}")]
 public async Task<IActionResult> SaveLookup(string kind, LookupRequest input, int? id = null) {
    if (kind == "pickup-locations") return BadRequest(new { error = "Campus pickup is fixed to Main gate, Canteen and Library." });
    if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest();
    var name = input.Name.Trim();
    switch (kind) {
      case "categories":
        if (await db.Categories.AnyAsync(x => x.CategoryName == name && x.Id != id)) return Conflict(new { error = "Name already exists." });
        var c = id.HasValue ? await db.Categories.FindAsync(id.Value) : new Category(); if (c == null) return NotFound(); c.CategoryName = name; if (!id.HasValue) db.Categories.Add(c); break;
      case "departments":
        if (string.IsNullOrWhiteSpace(input.Code)) return BadRequest(new { error = "Department code is required." });
        if (await db.Departments.AnyAsync(x => x.Code == input.Code.Trim().ToUpper() && x.Id != id)) return Conflict(new { error = "Code already exists." });
        var d = id.HasValue ? await db.Departments.FindAsync(id.Value) : new Department(); if (d == null) return NotFound(); d.DepartmentName = name; d.Code = input.Code.Trim().ToUpper(); if (!id.HasValue) db.Departments.Add(d); break;
      case "pickup-locations":
        if (await db.PickupLocations.AnyAsync(x => x.LocationName == name && x.Id != id)) return Conflict(new { error = "Name already exists." });
        var p = id.HasValue ? await db.PickupLocations.FindAsync(id.Value) : new PickupLocation(); if (p == null) return NotFound(); p.LocationName = name; if (!id.HasValue) db.PickupLocations.Add(p); break;
      default: return NotFound();
    }
    await db.SaveChangesAsync(); return Ok();
 }
 [Authorize(Roles = "Admin"), HttpDelete("api/admin/lookups/{kind}/{id:int}")]
 public async Task<IActionResult> DeleteLookup(string kind, int id) {
    if (kind == "pickup-locations") return BadRequest(new { error = "The three campus pickup locations cannot be deleted." });
    switch(kind) {
      case "categories": if(await db.Products.AnyAsync(x=>x.CategoryId==id) || await db.WantedPosts.AnyAsync(x=>x.CategoryId==id)) return Conflict(new { error="Category is in use." }); var c=await db.Categories.FindAsync(id); if(c==null)return NotFound(); db.Categories.Remove(c); break;
      case "departments": var d=await db.Departments.FindAsync(id); if(d==null)return NotFound(); if(await db.Users.AnyAsync(x=>x.Department==d.Code)) return Conflict(new {error="Department is in use."}); db.Departments.Remove(d); break;
      case "pickup-locations": if(await db.Orders.AnyAsync(x=>x.PickupLocationId==id))return Conflict(new {error="Location is in use."}); var p=await db.PickupLocations.FindAsync(id); if(p==null)return NotFound(); db.PickupLocations.Remove(p); break;
      default:return NotFound();
    } await db.SaveChangesAsync(); return Ok();
 }
}
