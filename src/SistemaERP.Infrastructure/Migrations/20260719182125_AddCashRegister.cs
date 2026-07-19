using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashRegisters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashRegisterNumber = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    OpeningAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OpeningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosingAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ClosingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Difference = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashRegisterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashMovements_CashRegisters_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "CashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashMovements_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_SaleId",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "SaleId" },
                unique: true,
                filter: "\"SaleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_SaleId",
                table: "CashMovements",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_OpeningDate",
                table: "CashRegisters",
                column: "OpeningDate");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_Status",
                table: "CashRegisters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_TenantId_CashRegisterNumber",
                table: "CashRegisters",
                columns: new[] { "TenantId", "CashRegisterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_UserId",
                table: "CashRegisters",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashMovements");

            migrationBuilder.DropTable(
                name: "CashRegisters");
        }
    }
}
