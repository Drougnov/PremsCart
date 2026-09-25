using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PremsCart.Api.Data.Migrations;

[DbContext(typeof(PremsCartDbContext))]
[Migration("20260923010000_AddEmailVerifications")]
public sealed class AddEmailVerifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmailVerifications",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(nullable: false),
                CodeHash = table.Column<string>(nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FailedAttempts = table.Column<int>(nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailVerifications", x => x.Id);
                table.ForeignKey("FK_EmailVerifications_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_EmailVerifications_UserId", "EmailVerifications", "UserId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("EmailVerifications");
}
