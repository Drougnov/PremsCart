using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace PremsCart.Api.Data.Migrations;
[DbContext(typeof(PremsCartDbContext)), Migration("20260925120000_FlexibleListings")]
public sealed class FlexibleListings:Migration {
 protected override void Up(MigrationBuilder m)=>m.Sql("""
ALTER TABLE "Products" ADD COLUMN "AllowRent" boolean NOT NULL DEFAULT false;
ALTER TABLE "Products" ADD COLUMN "RentalPrice" numeric(12,2);
ALTER TABLE "Products" ADD CONSTRAINT "CK_Products_RentalOption" CHECK (NOT "AllowRent" OR ("TransactionType" = 'Sell' AND "RentalPrice" > 0 AND "RentalPrice" IS NOT NULL));
""");
 protected override void Down(MigrationBuilder m)=>m.Sql("""
ALTER TABLE "Products" DROP CONSTRAINT "CK_Products_RentalOption";
ALTER TABLE "Products" DROP COLUMN "AllowRent", DROP COLUMN "RentalPrice";
""");
}
