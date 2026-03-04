using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Expenses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_Date",
                table: "Transactions",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_Type_Date",
                table: "Transactions",
                columns: new[] { "UserId", "Type", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_UserId_IncomeDate",
                table: "Incomes",
                columns: new[] { "UserId", "IncomeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId_ExpenseDate",
                table: "Expenses",
                columns: new[] { "UserId", "ExpenseDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId_Type_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_UserId_IncomeDate",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_UserId_ExpenseDate",
                table: "Expenses");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Expenses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
