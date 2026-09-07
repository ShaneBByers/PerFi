using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetirementAgeToUserConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetirementAge",
                table: "UserConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 65);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetirementAge",
                table: "UserConfigurations");
        }
    }
}
