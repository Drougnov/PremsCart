using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace PremsCart.Api.Data.Migrations;
[DbContext(typeof(PremsCartDbContext)), Migration("20260924140000_MarketplaceExperience")]
public sealed class MarketplaceExperience:Migration {
 protected override void Up(MigrationBuilder m)=>m.Sql("""
ALTER TABLE "Users" ADD COLUMN "NotifyMessages" boolean NOT NULL DEFAULT true, ADD COLUMN "NotifyOffers" boolean NOT NULL DEFAULT true, ADD COLUMN "NotifyOrders" boolean NOT NULL DEFAULT true, ADD COLUMN "NotifyRentals" boolean NOT NULL DEFAULT true, ADD COLUMN "NotifySavedListings" boolean NOT NULL DEFAULT true;
ALTER TABLE "Orders" ADD COLUMN "RentalStartDate" date;
""");
 protected override void Down(MigrationBuilder m)=>m.Sql("""
ALTER TABLE "Orders" DROP COLUMN "RentalStartDate";
ALTER TABLE "Users" DROP COLUMN "NotifyMessages", DROP COLUMN "NotifyOffers", DROP COLUMN "NotifyOrders", DROP COLUMN "NotifyRentals", DROP COLUMN "NotifySavedListings";
""");
}
