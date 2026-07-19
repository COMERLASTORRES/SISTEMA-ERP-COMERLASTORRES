using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CashMovementUniqueIndexIncludeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_PurchaseId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_SaleId",
                table: "CashMovements");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_PurchaseId_Type",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "PurchaseId", "Type" },
                unique: true,
                filter: "\"PurchaseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_SaleId_Type",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "SaleId", "Type" },
                unique: true,
                filter: "\"SaleId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_PurchaseId_Type",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_SaleId_Type",
                table: "CashMovements");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_PurchaseId",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "PurchaseId" },
                unique: true,
                filter: "\"PurchaseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_SaleId",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "SaleId" },
                unique: true,
                filter: "\"SaleId\" IS NOT NULL");
        }
    }
}
