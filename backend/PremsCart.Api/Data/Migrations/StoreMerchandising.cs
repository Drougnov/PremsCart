using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PremsCart.Api.Data.Migrations;

[DbContext(typeof(PremsCartDbContext)), Migration("20260925220000_StoreMerchandising")]
public sealed class StoreMerchandising : Migration
{
    protected override void Up(MigrationBuilder m) => m.Sql("""
ALTER TABLE "StoreProducts" ADD COLUMN "IsVisible" boolean NOT NULL DEFAULT true;
ALTER TABLE "StoreProducts" ADD COLUMN "SortOrder" integer NOT NULL DEFAULT 0;
ALTER TABLE "StoreProducts" ADD CONSTRAINT "CK_StoreProducts_SortOrder" CHECK ("SortOrder" >= 0);

-- A seller's store mirrors their posted items automatically. Backfill older posts too.
INSERT INTO "StoreProducts" ("StoreId", "ProductId", "Quantity", "IsVisible", "SortOrder")
SELECT s."Id", p."Id", 1, true, 0
FROM "Stores" s
JOIN "Products" p ON p."SellerId" = s."OwnerId"
WHERE NOT EXISTS (
    SELECT 1 FROM "StoreProducts" sp WHERE sp."ProductId" = p."Id"
);

WITH ranked AS (
    SELECT sp."Id", ROW_NUMBER() OVER (
        PARTITION BY sp."StoreId" ORDER BY p."CreatedAt" DESC, p."Id" DESC
    ) - 1 AS position
    FROM "StoreProducts" sp
    JOIN "Products" p ON p."Id" = sp."ProductId"
)
UPDATE "StoreProducts" sp
SET "SortOrder" = ranked.position
FROM ranked
WHERE ranked."Id" = sp."Id";
""");

    protected override void Down(MigrationBuilder m) => m.Sql("""
ALTER TABLE "StoreProducts" DROP CONSTRAINT IF EXISTS "CK_StoreProducts_SortOrder";
ALTER TABLE "StoreProducts" DROP COLUMN "IsVisible", DROP COLUMN "SortOrder";
""");
}
