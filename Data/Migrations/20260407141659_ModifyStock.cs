using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "VendorStock");

            migrationBuilder.RenameColumn(
                name: "SellPrice",
                table: "VendorStock",
                newName: "TotalPurchasePrice");

            migrationBuilder.AddColumn<long>(
                name: "StockNumber",
                table: "VendorStock",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pricing",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockNumber = table.Column<long>(type: "bigint", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pricing", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pricing");

            migrationBuilder.DropIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock");

            migrationBuilder.DropColumn(
                name: "StockNumber",
                table: "VendorStock");

            migrationBuilder.RenameColumn(
                name: "TotalPurchasePrice",
                table: "VendorStock",
                newName: "SellPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "VendorStock",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock",
                column: "ProductId");
        }
    }
}
