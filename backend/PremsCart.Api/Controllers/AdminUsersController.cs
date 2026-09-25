using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;

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
