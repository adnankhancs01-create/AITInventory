using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class stockchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock");

            migrationBuilder.CreateIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock");

            migrationBuilder.CreateIndex(
                name: "IX_VendorStock_ProductId",
                table: "VendorStock",
                column: "ProductId",
                unique: true);
        }
    }
}
