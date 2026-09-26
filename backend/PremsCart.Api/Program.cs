using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PremsCart.Api.Data;
using PremsCart.Api.Models;
using PremsCart.Api.Authentication;
using PremsCart.Api.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173")
          .AllowAnyHeader().AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection before starting the API.");
builder.Services.AddDbContext<PremsCartDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<VerificationEmailSender>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatPresence>();
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Configure Jwt:Key using .NET user-secrets or environment variables.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PremsCart",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PremsCart.Web",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/chatHub") &&
                    context.Request.Query.TryGetValue("access_token", out var accessToken))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var version = context.Principal?.FindFirst("version")?.Value;
                var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<PremsCartDbContext>();
                if (!int.TryParse(id, out var userId) || string.IsNullOrEmpty(role) || !await db.Users
                    .AnyAsync(u => u.Id == userId && u.IsVerified && u.Status == "Active" && u.TokenVersion.ToString() == version &&
                        u.Role.RoleName == role))
                    context.Fail("Account is unavailable or permissions changed.");
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:AutoMigrate")) {
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<PremsCartDbContext>().Database.MigrateAsync();
}
if (builder.Configuration["Bootstrap:AdminEmail"] is string adminEmail && builder.Configuration["Bootstrap:AdminPassword"] is string adminPassword) {
    using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PremsCartDbContext>();
    if (!await db.Users.AnyAsync(x => x.RoleId == 4)) {
        if (adminPassword.Length < 12) throw new InvalidOperationException("Bootstrap password must contain at least 12 characters.");
        if (await db.Users.AnyAsync(x => x.Email == adminEmail)) throw new InvalidOperationException("Use a new email for bootstrap admin.");
        var admin = new User { FirstName = "Campus", LastName = "Admin", Email = adminEmail, UniversityEmail = adminEmail, RoleId = 4, IsVerified = true };
        admin.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>().HashPassword(admin, adminPassword);
        db.Users.Add(admin); await db.SaveChangesAsync();
    }
}
await DemoDataSeeder.SeedAsync(app.Services, builder.Configuration);
app.UseCors("Frontend");
app.UseDefaultFiles();
app.UseStaticFiles();
// Concurrent checkout/acceptance requests may conflict; let the client refresh and retry.
app.Use(async (context, next) => {
    try { await next(context); }
    catch (Exception ex) when (ex is Npgsql.PostgresException { SqlState: "40001" } || ex.InnerException is Npgsql.PostgresException { SqlState: "40001" }) {
        context.Response.StatusCode = 409;
        await context.Response.WriteAsJsonAsync(new { error = "This item changed during your request. Refresh and try again." });
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub", options => options.CloseOnAuthenticationExpiration = true);
app.MapFallbackToFile("index.html");
app.Run();
