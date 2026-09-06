using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDisplayOrderToDisplayOrderInGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "TransactionCategories",
                newName: "DisplayOrderInGroup");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "AccountTypes",
                newName: "DisplayOrderInGroup");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "Accounts",
                newName: "DisplayOrderInGroup");

            // The renamed columns previously held a single sequence across every parent group for the user;
            // renumber each row 1..N within its own parent so the value is meaningful as "order within group".
            migrationBuilder.Sql(
                """
                ;WITH Numbered AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY TransactionCategoryGroupId ORDER BY DisplayOrderInGroup, Id) AS NewOrder
                    FROM TransactionCategories
                )
                UPDATE c
                SET c.DisplayOrderInGroup = n.NewOrder
                FROM TransactionCategories c
                INNER JOIN Numbered n ON c.Id = n.Id;
                """);

            migrationBuilder.Sql(
                """
                ;WITH Numbered AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY AccountTypeGroupId ORDER BY DisplayOrderInGroup, Id) AS NewOrder
                    FROM AccountTypes
                )
                UPDATE t
                SET t.DisplayOrderInGroup = n.NewOrder
                FROM AccountTypes t
                INNER JOIN Numbered n ON t.Id = n.Id;
                """);

            migrationBuilder.Sql(
                """
                ;WITH Numbered AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY InstitutionId ORDER BY DisplayOrderInGroup, Id) AS NewOrder
                    FROM Accounts
                )
                UPDATE a
                SET a.DisplayOrderInGroup = n.NewOrder
                FROM Accounts a
                INNER JOIN Numbered n ON a.Id = n.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: the per-group renumbering done in Up() is not reversed here - the original
            // cross-group global ordering can't be reconstructed from the per-group values alone.
            migrationBuilder.RenameColumn(
                name: "DisplayOrderInGroup",
                table: "TransactionCategories",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "DisplayOrderInGroup",
                table: "AccountTypes",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "DisplayOrderInGroup",
                table: "Accounts",
                newName: "DisplayOrder");
        }
    }
}
