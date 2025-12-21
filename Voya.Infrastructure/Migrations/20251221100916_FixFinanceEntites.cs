using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voya.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFinanceEntites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "GlobalSettings",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WalletTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Campaigns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_CreatorId",
                table: "Campaigns",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_Users_CreatorId",
                table: "Campaigns",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_Users_CreatorId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_CreatorId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "GlobalSettings",
                newName: "LastUpdated");
        }
    }
}
