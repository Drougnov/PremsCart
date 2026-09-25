using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
namespace PremsCart.Api.Controllers;
public record NotificationPreferences(bool Messages, bool Offers, bool Orders, bool Rentals, bool SavedListings);
[ApiController, Authorize, Route("api/users/preferences")]
public sealed class PreferencesController(PremsCartDbContext db):ControllerBase {
    int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> Get(){var u=await db.Users.FindAsync(Me);if(u==null)return NotFound();return Ok(new NotificationPreferences(u.NotifyMessages,u.NotifyOffers,u.NotifyOrders,u.NotifyRentals,u.NotifySavedListings));}
    [HttpPut]
    public async Task<IActionResult> Save(NotificationPreferences input){var u=await db.Users.FindAsync(Me);if(u==null)return NotFound();u.NotifyMessages=input.Messages;u.NotifyOffers=input.Offers;u.NotifyOrders=input.Orders;u.NotifyRentals=input.Rentals;u.NotifySavedListings=input.SavedListings;await db.SaveChangesAsync();return Ok(input);}
}
[ApiController, Route("api/discovery")]
public sealed class DiscoveryController(PremsCartDbContext db):ControllerBase {
    [AllowAnonymous, HttpGet("stats")]
    public async Task<IActionResult> Stats()=>Ok(new { verifiedMembers=await db.Users.CountAsync(u=>u.IsVerified&&u.Status=="Active"&&(u.RoleId==1||u.RoleId==2)), activeListings=await db.Products.CountAsync(p=>p.Status=="Available"&&!p.IsHidden&&p.Seller.Status=="Active"&&p.Seller.IsVerified&&!db.StoreProducts.Any(s=>s.ProductId==p.Id&&s.Store.IsHidden)), completedExchanges=await db.Orders.CountAsync(o=>o.Status=="Completed") });
}
