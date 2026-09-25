using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PremsCart.Api.Authentication;
using PremsCart.Api.Data;
using PremsCart.Api.Models;

namespace PremsCart.Api.Controllers;

public sealed record RegisterRequest(
    [Required, StringLength(80)] string FirstName,
    [Required, StringLength(80)] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);
public sealed record LoginRequest([Required] string Email, [Required] string Password);
public sealed record VerifyRequest([Required] string Email, [Required] string Code);
public sealed record UpdateProfileRequest([Required, StringLength(80)] string FirstName, [Required, StringLength(80)] string LastName);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    PremsCartDbContext db,
    IPasswordHasher<User> hasher,
    JwtTokenService tokens,
    VerificationEmailSender emailSender,
    IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var address = request.Email.Trim().ToLowerInvariant();
        var domain = address.Split('@').Last();
        var department = config.GetSection("University:Departments")[domain] ?? (domain == "bscse.puc.ac.bd" ? "CSE" : null);
        if (department is null || !Regex.IsMatch(address, @"^[a-z][a-z0-9.]*_[0-9]{5}@[a-z0-9.]+$"))
            return BadRequest(new { error = "Use your configured Premier University student email, such as name_44009@bscse.puc.ac.bd." });
        if (!await db.Departments.AnyAsync(x => x.Code == department)) return BadRequest(new { error = "This department is not enabled. Contact the campus administrator." });
        if (request.Password.Length > 128)
            return BadRequest(new { error = "Password is too long." });
        if (await db.Users.AnyAsync(x => x.Email == address || x.UniversityEmail == address))
            return Conflict(new { error = "This email is already registered." });

        var student = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = address,
            UniversityEmail = address,
            Batch = int.Parse(address.Split('_')[1][..2]),
            StudentIdLastThreeDigits = address.Split('_')[1].Split('@')[0][^3..],
            Program = "BS",
            Department = department,
            RoleId = 1,
            IsVerified = false
        };
        if (student.FirstName.Length == 0 || student.LastName.Length == 0)
            return BadRequest(new { error = "Enter your first and last name." });
        student.PasswordHash = hasher.HashPassword(student, request.Password);
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Users.Add(student);
        try
        {
            await db.SaveChangesAsync();
            db.EmailVerifications.Add(new EmailVerification
            {
                UserId = student.Id, CodeHash = HashCode(student.Id, code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });
            await db.SaveChangesAsync();
            await emailSender.SendAsync(address, code);
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new { error = "This email is already registered." });
        }
        return Accepted(new { message = "Account created. Check your university email for a verification code." });
    }

    [HttpPost("resend-code")]
    public async Task<IActionResult> Resend(EmailRequest request)
    {
        var address = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == address);
        if (user is null || user.IsVerified)
            return Ok(new { message = "If an unverified account exists, a new code will be sent." });
        var verification = await db.EmailVerifications.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (verification is not null && verification.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
            return StatusCode(429, new { error = "Wait one minute before requesting another code." });
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        verification ??= new EmailVerification { UserId = user.Id };
        verification.CodeHash = HashCode(user.Id, code);
        verification.CreatedAt = DateTime.UtcNow;
        verification.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
        verification.FailedAttempts = 0;
        if (verification.Id == 0) db.EmailVerifications.Add(verification);
        await emailSender.SendAsync(address, code);
        await db.SaveChangesAsync();
        return Ok(new { message = "If an unverified account exists, a new code will be sent." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> Verify(VerifyRequest request)
    {
        var address = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == address);
        if (user is null || user.IsVerified) return BadRequest(new { error = "Invalid or already verified account." });
        var verification = await db.EmailVerifications.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (verification is null || verification.ExpiresAt < DateTime.UtcNow || verification.FailedAttempts >= 5)
            return BadRequest(new { error = "Code expired or locked. Request a new code." });
        if (!Regex.IsMatch(request.Code, @"^[0-9]{6}$") ||
            !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(verification.CodeHash), Convert.FromHexString(HashCode(user.Id, request.Code))))
        {
            verification.FailedAttempts++;
            await db.SaveChangesAsync();
            return BadRequest(new { error = "Incorrect verification code." });
        }
        user.IsVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        db.EmailVerifications.Remove(verification);
        await db.SaveChangesAsync();
        return Ok(new { message = "University email verified. You can now sign in." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var address = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == address);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid email or password." });
        if (user.Status != "Active") return StatusCode(403, new { error = "Account is suspended. Contact campus moderation." });
        if (!user.IsVerified) return StatusCode(403, new { error = "Verify your university email before signing in." });
        return Ok(new { token = tokens.Create(user), expiresInSeconds = 7200, profile = Profile(user) });
    }

    [Authorize]
    [HttpGet("/api/users/profile")]
    public async Task<IActionResult> GetProfile()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.Include(x => x.Role).SingleAsync(x => x.Id == id);
        return Ok(Profile(user));
    }

    [Authorize]
    [HttpPut("/api/users/profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { error = "Enter your first and last name." });
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.Include(x => x.Role).SingleAsync(x => x.Id == id);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(Profile(user));
    }

    private string HashCode(int userId, string code)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
        return Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{userId}:{code}")));
    }

    private static object Profile(User user) => new
    {
        user.Id, user.FirstName, user.LastName, user.UniversityEmail,
        user.Batch, user.Department, user.Program, user.StudentIdLastThreeDigits,
        user.ProfileImage, user.IsVerified, Role = user.Role.RoleName
    };
}

public sealed record EmailRequest([Required, EmailAddress] string Email);
