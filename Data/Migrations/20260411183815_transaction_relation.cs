using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class transaction_relation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorTransaction_VendorClient_ClientId",
                table: "VendorTransaction");

            migrationBuilder.AddColumn<int>(
                name: "VendorClientId",
                table: "VendorTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorTransaction_VendorClientId",
                table: "VendorTransaction",
                column: "VendorClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorTransaction_VendorClientDetail_ClientId",
                table: "VendorTransaction",
                column: "ClientId",
                principalTable: "VendorClientDetail",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorTransaction_VendorClient_VendorClientId",
                table: "VendorTransaction",
                column: "VendorClientId",
                principalTable: "VendorClient",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorTransaction_VendorClientDetail_ClientId",
                table: "VendorTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_VendorTransaction_VendorClient_VendorClientId",
                table: "VendorTransaction");

            migrationBuilder.DropIndex(
                name: "IX_VendorTransaction_VendorClientId",
                table: "VendorTransaction");

            migrationBuilder.DropColumn(
                name: "VendorClientId",
                table: "VendorTransaction");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorTransaction_VendorClient_ClientId",
                table: "VendorTransaction",
                column: "ClientId",
                principalTable: "VendorClient",
                principalColumn: "Id");
        }
    }
}
