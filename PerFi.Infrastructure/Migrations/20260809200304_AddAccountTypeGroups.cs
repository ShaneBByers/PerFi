using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTypeGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountTypeGroupId",
                table: "AccountTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AccountTypeGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypeGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTypes_AccountTypeGroupId",
                table: "AccountTypes",
                column: "AccountTypeGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountTypes_AccountTypeGroups_AccountTypeGroupId",
                table: "AccountTypes",
                column: "AccountTypeGroupId",
                principalTable: "AccountTypeGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountTypes_AccountTypeGroups_AccountTypeGroupId",
                table: "AccountTypes");

            migrationBuilder.DropTable(
                name: "AccountTypeGroups");

            migrationBuilder.DropIndex(
                name: "IX_AccountTypes_AccountTypeGroupId",
                table: "AccountTypes");

            migrationBuilder.DropColumn(
                name: "AccountTypeGroupId",
                table: "AccountTypes");
        }
    }
}
