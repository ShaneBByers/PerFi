using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountContributionPlanEffectiveDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountContributionPlans_AccountId_ContributorType",
                table: "AccountContributionPlans");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveDate",
                table: "AccountContributionPlans",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_AccountContributionPlans_AccountId_ContributorType_EffectiveDate",
                table: "AccountContributionPlans",
                columns: new[] { "AccountId", "ContributorType", "EffectiveDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountContributionPlans_AccountId_ContributorType_EffectiveDate",
                table: "AccountContributionPlans");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "AccountContributionPlans");

            migrationBuilder.CreateIndex(
                name: "IX_AccountContributionPlans_AccountId_ContributorType",
                table: "AccountContributionPlans",
                columns: new[] { "AccountId", "ContributorType" },
                unique: true);
        }
    }
}
