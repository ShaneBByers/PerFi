using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerFi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeEfRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountBalances_FinanceSnapshots_FinanceSnapshotEntityId",
                table: "AccountBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AccountTypes_TypeId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Institutions_InstitutionEntityId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_InstitutionEntityId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_TypeId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_AccountBalances_FinanceSnapshotEntityId",
                table: "AccountBalances");

            migrationBuilder.AddColumn<int>(
                name: "FinanceSnapshotId",
                table: "AccountBalances",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE Accounts SET AccountTypeId = TypeId WHERE AccountTypeId = 0");
            migrationBuilder.Sql("UPDATE Accounts SET InstitutionId = InstitutionEntityId WHERE InstitutionId IS NULL AND InstitutionEntityId IS NOT NULL");
            migrationBuilder.Sql("UPDATE AccountBalances SET FinanceSnapshotId = FinanceSnapshotEntityId WHERE FinanceSnapshotId IS NULL AND FinanceSnapshotEntityId IS NOT NULL");

            migrationBuilder.AlterColumn<int>(
                name: "InstitutionId",
                table: "Accounts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FinanceSnapshotId",
                table: "AccountBalances",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "InstitutionEntityId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TypeId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "FinanceSnapshotEntityId",
                table: "AccountBalances");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountTypeId",
                table: "Accounts",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_InstitutionId",
                table: "Accounts",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalances_FinanceSnapshotId",
                table: "AccountBalances",
                column: "FinanceSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountBalances_FinanceSnapshots_FinanceSnapshotId",
                table: "AccountBalances",
                column: "FinanceSnapshotId",
                principalTable: "FinanceSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AccountTypes_AccountTypeId",
                table: "Accounts",
                column: "AccountTypeId",
                principalTable: "AccountTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Institutions_InstitutionId",
                table: "Accounts",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountBalances_FinanceSnapshots_FinanceSnapshotId",
                table: "AccountBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AccountTypes_AccountTypeId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Institutions_InstitutionId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AccountTypeId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_InstitutionId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_AccountBalances_FinanceSnapshotId",
                table: "AccountBalances");

            migrationBuilder.DropColumn(
                name: "FinanceSnapshotId",
                table: "AccountBalances");

            migrationBuilder.AlterColumn<int>(
                name: "InstitutionId",
                table: "Accounts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "InstitutionEntityId",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypeId",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FinanceSnapshotEntityId",
                table: "AccountBalances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_InstitutionEntityId",
                table: "Accounts",
                column: "InstitutionEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TypeId",
                table: "Accounts",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalances_FinanceSnapshotEntityId",
                table: "AccountBalances",
                column: "FinanceSnapshotEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountBalances_FinanceSnapshots_FinanceSnapshotEntityId",
                table: "AccountBalances",
                column: "FinanceSnapshotEntityId",
                principalTable: "FinanceSnapshots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AccountTypes_TypeId",
                table: "Accounts",
                column: "TypeId",
                principalTable: "AccountTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Institutions_InstitutionEntityId",
                table: "Accounts",
                column: "InstitutionEntityId",
                principalTable: "Institutions",
                principalColumn: "Id");
        }
    }
}
