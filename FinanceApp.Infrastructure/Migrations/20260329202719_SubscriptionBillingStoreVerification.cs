using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionBillingStoreVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppleOriginalTransactionId",
                table: "AspNetUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GooglePurchaseToken",
                table: "AspNetUsers",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionBillingSource",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubscriptionExpiresAtUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionPurchaseRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BillingSource = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Plan = table.Column<int>(type: "int", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPurchaseRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchaseRecords_BillingSource_ExternalTransactionId",
                table: "SubscriptionPurchaseRecords",
                columns: new[] { "BillingSource", "ExternalTransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchaseRecords_UserId",
                table: "SubscriptionPurchaseRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPurchaseRecords");

            migrationBuilder.DropColumn(
                name: "AppleOriginalTransactionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GooglePurchaseToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SubscriptionBillingSource",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresAtUtc",
                table: "AspNetUsers");
        }
    }
}
