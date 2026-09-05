using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartCompareAPI.Migrations
{
    /// <inheritdoc />
    public partial class MakeStoreProductProductOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreProducts_Products_ProductId",
                table: "StoreProducts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "StoreProducts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreProducts_Products_ProductId",
                table: "StoreProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreProducts_Products_ProductId",
                table: "StoreProducts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "StoreProducts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreProducts_Products_ProductId",
                table: "StoreProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
