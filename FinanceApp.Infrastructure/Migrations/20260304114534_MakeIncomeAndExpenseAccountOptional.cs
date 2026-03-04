using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeIncomeAndExpenseAccountOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_Accounts_AccountId",
                table: "Incomes");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                table: "Incomes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "Expenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_AccountId",
                table: "Expenses",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TransactionId",
                table: "Expenses",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Accounts_AccountId",
                table: "Expenses",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Transactions_TransactionId",
                table: "Expenses",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_Accounts_AccountId",
                table: "Incomes",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Accounts_AccountId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Transactions_TransactionId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_Accounts_AccountId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_AccountId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TransactionId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Expenses");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                table: "Incomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_Accounts_AccountId",
                table: "Incomes",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
