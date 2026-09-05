using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserConfigurationAndAddContributionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConfigurationExpectations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_UserConfigurations_UserId",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "CurrentAnnualSalary",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "LastVerifiedDateTime",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "PayCycle",
                table: "UserConfigurations");

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualInflationPercentage",
                table: "UserConfigurations",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualSalaryRaisePercentage",
                table: "UserConfigurations",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PayCycleType",
                table: "UserConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReferencePayDate",
                table: "UserConfigurations",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAnnualGrowthPercentage",
                table: "Accounts",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AccountContributionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    ContributorType = table.Column<int>(type: "int", nullable: false),
                    DollarAmountPerPayCycle = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DollarAmountPerPayCycleAnnualIncrease = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DollarAmountAnnual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DollarAmountAnnualIncrease = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentagePerPayCycle = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PercentagePerPayCycleAnnualIncrease = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PercentageAnnual = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PercentageAnnualIncrease = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountContributionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountContributionPlans_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryProgressions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AnnualSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryProgressions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryProgressions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountContributionPlans_AccountId_ContributorType",
                table: "AccountContributionPlans",
                columns: new[] { "AccountId", "ContributorType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryProgressions_UserId_EffectiveDate",
                table: "SalaryProgressions",
                columns: new[] { "UserId", "EffectiveDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountContributionPlans");

            migrationBuilder.DropTable(
                name: "SalaryProgressions");

            migrationBuilder.DropColumn(
                name: "ExpectedAnnualInflationPercentage",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "ExpectedAnnualSalaryRaisePercentage",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "PayCycleType",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "ReferencePayDate",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "ExpectedAnnualGrowthPercentage",
                table: "Accounts");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentAnnualSalary",
                table: "UserConfigurations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastVerifiedDateTime",
                table: "UserConfigurations",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PayCycle",
                table: "UserConfigurations",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_UserConfigurations_UserId",
                table: "UserConfigurations",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "UserConfigurationExpectations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnualBrokerageContributionPerPayCycleIncrease = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualInflationPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AnnualSalaryRaisePercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AnnualStockMarketReturnPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EmployerMatchPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EmployerProfitSharingPercentage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
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
        }
    }
}
