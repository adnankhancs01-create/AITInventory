using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class addittionaltransactionsfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientAddress",
                table: "VendorTransaction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "VendorTransaction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TransactionNumber",
                table: "VendorTransaction",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientAddress",
                table: "VendorTransaction");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "VendorTransaction");

            migrationBuilder.DropColumn(
                name: "TransactionNumber",
                table: "VendorTransaction");
        }
    }
}
