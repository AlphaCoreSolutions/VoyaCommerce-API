using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voya.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class orderUpdateAndSplitBill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                table: "Shipments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCost",
                table: "Shipments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "ShippingAddressId",
                table: "Orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "MainImage",
                table: "OrderItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SplitBills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SplitBills_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SplitBillShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SplitBillId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentReference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitBillShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SplitBillShares_SplitBills_SplitBillId",
                        column: x => x.SplitBillId,
                        principalTable: "SplitBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SplitBillShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AddressId",
                table: "Shipments",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ShipmentId",
                table: "OrderItems",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SplitBills_CartId",
                table: "SplitBills",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_SplitBillShares_SplitBillId",
                table: "SplitBillShares",
                column: "SplitBillId");

            migrationBuilder.CreateIndex(
                name: "IX_SplitBillShares_UserId",
                table: "SplitBillShares",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Shipments_ShipmentId",
                table: "OrderItems",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Addresses_AddressId",
                table: "Shipments",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Shipments_ShipmentId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Addresses_AddressId",
                table: "Shipments");

            migrationBuilder.DropTable(
                name: "SplitBillShares");

            migrationBuilder.DropTable(
                name: "SplitBills");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_AddressId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ShipmentId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ShippingCost",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "MainImage",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "OrderItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "ShippingAddressId",
                table: "Orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
