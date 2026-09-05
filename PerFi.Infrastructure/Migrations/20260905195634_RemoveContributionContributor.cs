using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveContributionContributor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contributions_ContributionContributors_ContributorId",
                table: "Contributions");

            migrationBuilder.DropTable(
                name: "ContributionContributors");

            migrationBuilder.DropIndex(
                name: "IX_Contributions_ContributorId",
                table: "Contributions");

            migrationBuilder.RenameColumn(
                name: "ContributorId",
                table: "Contributions",
                newName: "Contributor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Contributor",
                table: "Contributions",
                newName: "ContributorId");

            migrationBuilder.CreateTable(
                name: "ContributionContributors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                name: "IX_Contributions_ContributorId",
                table: "Contributions",
                column: "ContributorId");

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
    }
}
