using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartCompareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalProductFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalKey",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageType",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Variant",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CanonicalKey",
                table: "Products",
                column: "CanonicalKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CanonicalKey",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CanonicalKey",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PackageType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Variant",
                table: "Products");
        }
    }
}
