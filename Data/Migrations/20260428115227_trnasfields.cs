using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class trnasfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReferenceNumber",
                table: "TransactionMst",
                type: "bigint",
                nullable: true);
            
            migrationBuilder.AddColumn<long>(
                name: "Discount",
                table: "TransactionDetails",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnTransaction_TransactionDetailId",
                table: "ReturnTransaction",
                column: "TransactionDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnTransaction_TransactionDetails_TransactionDetailId",
                table: "ReturnTransaction",
                column: "TransactionDetailId",
                principalTable: "TransactionDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnTransaction_TransactionDetails_TransactionDetailId",
                table: "ReturnTransaction");

            migrationBuilder.DropIndex(
                name: "IX_ReturnTransaction_TransactionDetailId",
                table: "ReturnTransaction");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "TransactionMst");

            migrationBuilder.AlterColumn<decimal>(
                name: "Discount",
                table: "TransactionDetails",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);
        }
    }
}
