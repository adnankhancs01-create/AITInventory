using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagement2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "UserMst",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UserMst",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UserMst",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                table: "UserMst",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "UserMst",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserMst",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRoot",
                table: "UserMst",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginOn",
                table: "UserMst",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "UserMst",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppScreen2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScreenKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRootOnly = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppScreen2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Business2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Business2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRole2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleScreenPermission2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ScreenKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleScreenPermission2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleScreenPermission2_UserRole2_RoleId",
                        column: x => x.RoleId,
                        principalTable: "UserRole2",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMst_BusinessId",
                table: "UserMst",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMst_Email",
                table: "UserMst",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserMst_IsRoot",
                table: "UserMst",
                column: "IsRoot",
                unique: true,
                filter: "[IsRoot] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UserMst_RoleId",
                table: "UserMst",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMst_UserName",
                table: "UserMst",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppScreen2_ScreenKey",
                table: "AppScreen2",
                column: "ScreenKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleScreenPermission2_RoleId_ScreenKey",
                table: "RoleScreenPermission2",
                columns: new[] { "RoleId", "ScreenKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole2_Name",
                table: "UserRole2",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMst_Business2_BusinessId",
                table: "UserMst",
                column: "BusinessId",
                principalTable: "Business2",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMst_UserRole2_RoleId",
                table: "UserMst",
                column: "RoleId",
                principalTable: "UserRole2",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserMst_Business2_BusinessId",
                table: "UserMst");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMst_UserRole2_RoleId",
                table: "UserMst");

            migrationBuilder.DropTable(
                name: "AppScreen2");

            migrationBuilder.DropTable(
                name: "Business2");

            migrationBuilder.DropTable(
                name: "RoleScreenPermission2");

            migrationBuilder.DropTable(
                name: "UserRole2");

            migrationBuilder.DropIndex(
                name: "IX_UserMst_BusinessId",
                table: "UserMst");

            migrationBuilder.DropIndex(
                name: "IX_UserMst_Email",
                table: "UserMst");

            migrationBuilder.DropIndex(
                name: "IX_UserMst_IsRoot",
                table: "UserMst");

            migrationBuilder.DropIndex(
                name: "IX_UserMst_RoleId",
                table: "UserMst");

            migrationBuilder.DropIndex(
                name: "IX_UserMst_UserName",
                table: "UserMst");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "UserMst");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "UserMst");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserMst");

            migrationBuilder.DropColumn(
                name: "IsRoot",
                table: "UserMst");

            migrationBuilder.DropColumn(
                name: "LastLoginOn",
                table: "UserMst");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "UserMst");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "UserMst",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UserMst",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UserMst",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
