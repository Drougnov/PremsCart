using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PremsCart.Api.Data.Migrations;

[DbContext(typeof(PremsCartDbContext))]
[Migration("20260923000000_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(InitialCreate).Assembly.GetManifestResourceStream(
            "PremsCart.Api.Data.Migrations.InitialSchema.sql")
            ?? throw new InvalidOperationException("Embedded initial schema not found.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse dependency order. This removes all marketplace data.
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS "Notifications", "Reports", "StoreProducts", "Stores",
            "Reviews", "Orders", "Offers", "Messages", "Conversations", "WantedPosts",
            "Wishlist", "ProductImages", "Products", "Users", "PickupLocations",
            "Categories", "Departments", "Universities", "Roles";
            """);
    }
}
