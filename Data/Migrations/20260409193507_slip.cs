using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class slip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionSlip",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    SlipContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionSlip", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionSlip_VendorTransaction_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "VendorTransaction",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSlip_TransactionId",
                table: "TransactionSlip",
                column: "TransactionId",
                unique: true,
                filter: "[TransactionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionSlip");
        }
    }
}
