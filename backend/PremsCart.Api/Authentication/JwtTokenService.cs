using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PremsCart.Api.Models;

namespace PremsCart.Api.Authentication;

public sealed class JwtTokenService(IConfiguration config)
{
    public string Create(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim("version", user.TokenVersion.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FirstName),
            new Claim(ClaimTypes.Email, user.UniversityEmail),
            new Claim(ClaimTypes.Role, user.Role.RoleName)
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "PremsCart",
            audience: config["Jwt:Audience"] ?? "PremsCart.Web",
            claims: claims, expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
