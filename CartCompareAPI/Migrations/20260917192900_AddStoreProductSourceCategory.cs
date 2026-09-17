using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartCompareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProductSourceCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceCategoryId",
                table: "StoreProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProducts_SourceCategoryId",
                table: "StoreProducts",
                column: "SourceCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreProducts_Categories_SourceCategoryId",
                table: "StoreProducts",
                column: "SourceCategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreProducts_Categories_SourceCategoryId",
                table: "StoreProducts");

            migrationBuilder.DropIndex(
                name: "IX_StoreProducts_SourceCategoryId",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "SourceCategoryId",
                table: "StoreProducts");
        }
    }
}
