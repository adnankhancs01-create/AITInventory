using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class ttransmstt_fileds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorStock_Products_ProductId",
                table: "VendorStock");

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "TransactionMst",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetails_ProductId",
                table: "TransactionDetails",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetails_Products_ProductId",
                table: "TransactionDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VendorStock_Products_ProductId",
                table: "VendorStock",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetails_Products_ProductId",
                table: "TransactionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorStock_Products_ProductId",
                table: "VendorStock");

            migrationBuilder.DropIndex(
                name: "IX_TransactionDetails_ProductId",
                table: "TransactionDetails");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "TransactionMst");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorStock_Products_ProductId",
                table: "VendorStock",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
