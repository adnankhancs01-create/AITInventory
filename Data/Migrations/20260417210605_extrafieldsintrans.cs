using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class extrafieldsintrans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "TransactionMst",
                newName: "TotalDiscount");

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "TransactionDetails",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount",
                table: "TransactionDetails");

            migrationBuilder.RenameColumn(
                name: "TotalDiscount",
                table: "TransactionMst",
                newName: "Discount");
        }
    }
}
