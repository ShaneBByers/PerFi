using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToAccountTypeGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AccountTypeGroups",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTypeGroups_UserId",
                table: "AccountTypeGroups",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountTypeGroups_AspNetUsers_UserId",
                table: "AccountTypeGroups",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountTypeGroups_AspNetUsers_UserId",
                table: "AccountTypeGroups");

            migrationBuilder.DropIndex(
                name: "IX_AccountTypeGroups_UserId",
                table: "AccountTypeGroups");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AccountTypeGroups");
        }
    }
}
