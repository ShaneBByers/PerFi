using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Institutions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "AccountTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "AccountTypeGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE i
SET DisplayOrder = ordered.RowNum
FROM Institutions i
INNER JOIN (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Name, Id) AS RowNum
    FROM Institutions
) AS ordered ON ordered.Id = i.Id;
");

            migrationBuilder.Sql(@"
UPDATE g
SET DisplayOrder = ordered.RowNum
FROM AccountTypeGroups g
INNER JOIN (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Name, Id) AS RowNum
    FROM AccountTypeGroups
) AS ordered ON ordered.Id = g.Id;
");

            migrationBuilder.Sql(@"
UPDATE t
SET DisplayOrder = ordered.RowNum
FROM AccountTypes t
INNER JOIN (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Name, Id) AS RowNum
    FROM AccountTypes
) AS ordered ON ordered.Id = t.Id;
");

            migrationBuilder.Sql(@"
UPDATE a
SET DisplayOrder = ordered.RowNum
FROM Accounts a
INNER JOIN (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Name, Id) AS RowNum
    FROM Accounts
) AS ordered ON ordered.Id = a.Id;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "AccountTypes");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "AccountTypeGroups");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Accounts");
        }
    }
}
