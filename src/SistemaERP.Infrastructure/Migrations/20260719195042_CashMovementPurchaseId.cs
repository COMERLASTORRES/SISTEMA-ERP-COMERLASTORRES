using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CashMovementPurchaseId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseId",
                table: "CashMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_PurchaseId",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "PurchaseId" },
                unique: true,
                filter: "\"PurchaseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_PurchaseId",
                table: "CashMovements",
                column: "PurchaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashMovements_Purchases_PurchaseId",
                table: "CashMovements",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashMovements_Purchases_PurchaseId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_PurchaseId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_PurchaseId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "CashMovements");
        }
    }
}
