using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voya.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExternalTrackingNumber",
                table: "Shipments",
                newName: "TrackingNumber");

            migrationBuilder.RenameColumn(
                name: "EstimatedDelivery",
                table: "Shipments",
                newName: "ActualDeliveryTime");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProviderId",
                table: "Shipments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CurrentLocation",
                table: "Shipments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DriverId",
                table: "Shipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryTime",
                table: "Shipments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Shipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                table: "NexusRoles",
                type: "text",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DriverId",
                table: "Shipments",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_DriverProfiles_DriverId",
                table: "Shipments",
                column: "DriverId",
                principalTable: "DriverProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_DriverProfiles_DriverId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_DriverId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryTime",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "TrackingNumber",
                table: "Shipments",
                newName: "ExternalTrackingNumber");

            migrationBuilder.RenameColumn(
                name: "ActualDeliveryTime",
                table: "Shipments",
                newName: "EstimatedDelivery");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProviderId",
                table: "Shipments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "Permissions",
                table: "NexusRoles",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
