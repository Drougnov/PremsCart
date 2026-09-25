using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace PremsCart.Api.Data.Migrations;
[DbContext(typeof(PremsCartDbContext)), Migration("20260924120000_ShoppingAndRentals")]
public sealed class ShoppingAndRentals : Migration {
 protected override void Up(MigrationBuilder m) => m.Sql("""
ALTER TABLE "Products" ADD COLUMN "IsHidden" boolean NOT NULL DEFAULT false;
ALTER TABLE "WantedPosts" ADD COLUMN "IsHidden" boolean NOT NULL DEFAULT false;
ALTER TABLE "Stores" ADD COLUMN "IsHidden" boolean NOT NULL DEFAULT false;
ALTER TABLE "Orders" ADD COLUMN "RentalDays" integer, ADD COLUMN "RentalStartedAt" timestamptz, ADD COLUMN "RentalDueAt" timestamptz, ADD COLUMN "ReturnedAt" timestamptz;
UPDATE "Products" p SET "IsHidden"=true WHERE EXISTS (SELECT 1 FROM "Reports" r WHERE r."ReportedProductId"=p."Id" AND (r."ResolutionAction"='Hide listing' OR (r."ResolutionAction" IS NULL AND r."Status"='Resolved')));
-- Move all references to three canonical rows before removing obsolete locations.
-- Temporary unique names avoid collisions with custom entries on an existing database.
UPDATE "PickupLocations" SET "LocationName"='premscart-migration-' || "Id";
INSERT INTO "PickupLocations" ("Id","LocationName") VALUES (2,'premscart-migration-2'),(3,'premscart-migration-3'),(4,'premscart-migration-4') ON CONFLICT ("Id") DO NOTHING;
UPDATE "Orders" SET "PickupLocationId"=3 WHERE "PickupLocationId" IS NOT NULL AND "PickupLocationId" NOT IN (2,3,4);
DELETE FROM "PickupLocations" WHERE "Id" NOT IN (2,3,4);
UPDATE "PickupLocations" SET "LocationName"=CASE "Id" WHEN 2 THEN 'Library' WHEN 3 THEN 'Main gate' ELSE 'Canteen' END;
UPDATE "Products" SET "Location"=CASE WHEN lower(coalesce("Location",'')) LIKE '%library%' THEN 'Library' WHEN lower(coalesce("Location",'')) LIKE '%canteen%' OR lower(coalesce("Location",'')) LIKE '%cafeteria%' THEN 'Canteen' ELSE 'Main gate' END;
SELECT setval(pg_get_serial_sequence('"PickupLocations"','Id'),4);
-- Old rental rows have no duration. Close pending legacy requests; keep active history for manual review.
UPDATE "Orders" o SET "Status"='Cancelled' FROM "Products" p WHERE p."Id"=o."ProductId" AND p."TransactionType"='Rent' AND o."Status"='Pending';
""");
 protected override void Down(MigrationBuilder m) => throw new NotSupportedException("Restore a backup to revert this data migration.");
}
