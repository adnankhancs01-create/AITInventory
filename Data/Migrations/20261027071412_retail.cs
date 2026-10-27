using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class retail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetailPos2_Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailPos2_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RetailPos2_Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreditLimitOverridden = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailPos2_Sales", x => x.Id);
                    table.CheckConstraint("CK_RetailPos2_Sales_Total", "[TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_RetailPos2_Sales_RetailPos2_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "RetailPos2_Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetailPos2_Adjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailPos2_Adjustments", x => x.Id);
                    table.CheckConstraint("CK_RetailPos2_Adjustments_Amount", "[Amount] <> 0");
                    table.ForeignKey(
                        name: "FK_RetailPos2_Adjustments_RetailPos2_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "RetailPos2_Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetailPos2_Adjustments_RetailPos2_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "RetailPos2_Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetailPos2_Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedBy = table.Column<int>(type: "int", nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailPos2_Payments", x => x.Id);
                    table.CheckConstraint("CK_RetailPos2_Payments_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_RetailPos2_Payments_RetailPos2_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "RetailPos2_Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetailPos2_Payments_RetailPos2_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "RetailPos2_Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetailPos2_SaleLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailPos2_SaleLines", x => x.Id);
                    table.CheckConstraint("CK_RetailPos2_SaleLines_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_RetailPos2_SaleLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetailPos2_SaleLines_RetailPos2_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "RetailPos2_Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Adjustments_ClientId",
                table: "RetailPos2_Adjustments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Adjustments_SaleId",
                table: "RetailPos2_Adjustments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Clients_Name",
                table: "RetailPos2_Clients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Clients_Phone",
                table: "RetailPos2_Clients",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Payments_ClientId_PaymentDate",
                table: "RetailPos2_Payments",
                columns: new[] { "ClientId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Payments_IdempotencyKey",
                table: "RetailPos2_Payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Payments_PaymentNumber",
                table: "RetailPos2_Payments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Payments_SaleId",
                table: "RetailPos2_Payments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_SaleLines_ProductId",
                table: "RetailPos2_SaleLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_SaleLines_SaleId",
                table: "RetailPos2_SaleLines",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Sales_ClientId_SaleDate",
                table: "RetailPos2_Sales",
                columns: new[] { "ClientId", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Sales_IdempotencyKey",
                table: "RetailPos2_Sales",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetailPos2_Sales_InvoiceNumber",
                table: "RetailPos2_Sales",
                column: "InvoiceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailPos2_Adjustments");

            migrationBuilder.DropTable(
                name: "RetailPos2_Payments");

            migrationBuilder.DropTable(
                name: "RetailPos2_SaleLines");

            migrationBuilder.DropTable(
                name: "RetailPos2_Sales");

            migrationBuilder.DropTable(
                name: "RetailPos2_Clients");
        }
    }
}
