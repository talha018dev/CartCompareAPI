using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartCompareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProductAndPriceHistoryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreProducts_StoreId",
                table: "StoreProducts");

            migrationBuilder.DropIndex(
                name: "IX_PriceHistory_StoreProductId",
                table: "PriceHistory");

            migrationBuilder.RenameColumn(
                name: "DiscountPrice",
                table: "PriceHistory",
                newName: "OriginalPrice");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProducts_StoreId_ExternalProductId",
                table: "StoreProducts",
                columns: new[] { "StoreId", "ExternalProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_StoreProductId_RecordedAt",
                table: "PriceHistory",
                columns: new[] { "StoreProductId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreProducts_StoreId_ExternalProductId",
                table: "StoreProducts");

            migrationBuilder.DropIndex(
                name: "IX_PriceHistory_StoreProductId_RecordedAt",
                table: "PriceHistory");

            migrationBuilder.RenameColumn(
                name: "OriginalPrice",
                table: "PriceHistory",
                newName: "DiscountPrice");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProducts_StoreId",
                table: "StoreProducts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_StoreProductId",
                table: "PriceHistory",
                column: "StoreProductId");
        }
    }
}
