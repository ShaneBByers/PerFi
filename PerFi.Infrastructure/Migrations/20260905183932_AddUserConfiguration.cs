using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PayCycle = table.Column<TimeSpan>(type: "time", nullable: false),
                    CurrentAnnualSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastVerifiedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurations", x => x.Id);
                    table.UniqueConstraint("AK_UserConfigurations_UserId", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserConfigurations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserConfigurationExpectations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnualSalaryRaisePercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EmployerMatchPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EmployerProfitSharingPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AnnualBrokerageContributionPerPayCycleIncrease = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualStockMarketReturnPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AnnualInflationPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LastVerifiedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurationExpectations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConfigurationExpectations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserConfigurationExpectations_UserConfigurations_UserId",
                        column: x => x.UserId,
                        principalTable: "UserConfigurations",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserConfigurationExpectations_UserId",
                table: "UserConfigurationExpectations",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConfigurations_UserId",
                table: "UserConfigurations",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConfigurationExpectations");

            migrationBuilder.DropTable(
                name: "UserConfigurations");
        }
    }
}
