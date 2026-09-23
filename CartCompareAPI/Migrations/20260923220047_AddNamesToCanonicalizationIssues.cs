using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartCompareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNamesToCanonicalizationIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "StoreProductCanonicalizationIssues",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoreProductName",
                table: "StoreProductCanonicalizationIssues",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "StoreProductCanonicalizationIssues");

            migrationBuilder.DropColumn(
                name: "StoreProductName",
                table: "StoreProductCanonicalizationIssues");
        }
    }
}
