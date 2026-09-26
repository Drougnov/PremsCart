using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController(PremsCartDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Users.Include(x => x.Role).OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.UniversityEmail, x.IsVerified, x.Status, Role = x.Role.RoleName })
            .ToListAsync());

    [HttpPost("{id:int}/verify")]
    public async Task<IActionResult> Verify(int id)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        if (user.IsVerified) return NoContent();

        user.IsVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.TokenVersion++;
        var codes = await db.EmailVerifications.Where(x => x.UserId == id).ToListAsync();
        if (codes.Count > 0) db.EmailVerifications.RemoveRange(codes);
        db.Notifications.Add(new Notification
        {
            UserId = id,
            Title = "Account verified",
            Message = "An administrator verified your campus account. You can now use PremsCart.",
            Link = "/dashboard",
            Type = "account"
        });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, ChangeRoleRequest request)
    {
        var role = await db.Roles.SingleOrDefaultAsync(x => x.RoleName == request.Role);
        if (role is null) return BadRequest(new { error = "Unknown role." });
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (!user.IsVerified) return BadRequest(new { error = "Verify the account before assigning a role." });
        if (user.RoleId == 4 && role.Id != 4 && await db.Users.CountAsync(x => x.RoleId == 4 && x.IsVerified && x.Status == "Active") <= 1)
            return BadRequest(new { error = "The last admin cannot be demoted." });
        user.RoleId = role.Id;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public sealed record ChangeRoleRequest(string Role);
