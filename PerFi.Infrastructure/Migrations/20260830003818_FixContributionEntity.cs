using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixContributionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contributions_Contributors_ContributorId",
                table: "Contributions");

            migrationBuilder.DropTable(
                name: "Contributors");

            migrationBuilder.CreateTable(
                name: "ContributionContributors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionContributors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionContributors_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContributionContributors_UserId",
                table: "ContributionContributors",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contributions_ContributionContributors_ContributorId",
                table: "Contributions",
                column: "ContributorId",
                principalTable: "ContributionContributors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contributions_ContributionContributors_ContributorId",
                table: "Contributions");

            migrationBuilder.DropTable(
                name: "ContributionContributors");

            migrationBuilder.CreateTable(
                name: "Contributors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contributors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contributors_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contributors_UserId",
                table: "Contributors",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contributions_Contributors_ContributorId",
                table: "Contributions",
                column: "ContributorId",
                principalTable: "Contributors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
