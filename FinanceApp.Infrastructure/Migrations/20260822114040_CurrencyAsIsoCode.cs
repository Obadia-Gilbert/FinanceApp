using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <summary>
    /// Switches the `Currency` column on every financial table from the C# enum's ordinal
    /// int (0, 1, 2, …) to its ISO-4217 string code ("USD", "EUR", "TZS", …).
    ///
    /// Hand-written rather than scaffolded as-is: a plain ALTER COLUMN int -> nvarchar(3)
    /// would cast each existing value to its numeric string ("2"), not its currency code
    /// ("TZS") — silently corrupting every row. This instead adds a new column, populates
    /// it via an explicit CASE mapping, drops the old column, and renames the new one into
    /// place, so existing data keeps its real meaning.
    /// </summary>
    /// <inheritdoc />
    public partial class CurrencyAsIsoCode : Migration
    {
        // Ordinal -> code must match FinanceApp.Domain.Enums.Currency exactly.
        private static readonly (int Ordinal, string Code)[] CurrencyMap =
        [
            (0, "USD"), (1, "EUR"), (2, "TZS"), (3, "GBP"),
            (4, "JPY"), (5, "AUD"), (6, "CAD"), (7, "CHF"),
            (8, "UGX"), (9, "KES"), (10, "RWF"), (11, "ZAR"),
            (12, "CNY"), (13, "INR"), (14, "BRL"), (15, "MXN"),
        ];

        private static readonly string[] Tables =
        [
            "Accounts", "Budgets", "CategoryBudgets", "Expenses", "Incomes", "RecurringTemplates", "Transactions"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(
                    name: "CurrencyCode",
                    table: table,
                    type: "nvarchar(3)",
                    maxLength: 3,
                    nullable: true);

                var cases = string.Join(" ", CurrencyMap.Select(m => $"WHEN {m.Ordinal} THEN '{m.Code}'"));
                migrationBuilder.Sql($"UPDATE [{table}] SET [CurrencyCode] = CASE [Currency] {cases} END;");

                migrationBuilder.DropColumn(name: "Currency", table: table);
                migrationBuilder.RenameColumn(name: "CurrencyCode", table: table, newName: "Currency");

                migrationBuilder.AlterColumn<string>(
                    name: "Currency",
                    table: table,
                    type: "nvarchar(3)",
                    maxLength: 3,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(3)",
                    oldMaxLength: 3,
                    oldNullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<int>(
                    name: "CurrencyOrdinal",
                    table: table,
                    type: "int",
                    nullable: true);

                var cases = string.Join(" ", CurrencyMap.Select(m => $"WHEN '{m.Code}' THEN {m.Ordinal}"));
                migrationBuilder.Sql($"UPDATE [{table}] SET [CurrencyOrdinal] = CASE [Currency] {cases} END;");

                migrationBuilder.DropColumn(name: "Currency", table: table);
                migrationBuilder.RenameColumn(name: "CurrencyOrdinal", table: table, newName: "Currency");

                migrationBuilder.AlterColumn<int>(
                    name: "Currency",
                    table: table,
                    type: "int",
                    nullable: false,
                    oldClrType: typeof(int),
                    oldType: "int",
                    oldNullable: true);
            }
        }
    }
}
