using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CartCompareAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProductCanonicalizationIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreProductCanonicalizationIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    FirstOccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastOccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProductCanonicalizationIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreProductCanonicalizationIssues_StoreProducts_StoreProdu~",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreProductCanonicalizationIssues_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductCanonicalizationIssues_StoreId_LastOccurredAt",
                table: "StoreProductCanonicalizationIssues",
                columns: new[] { "StoreId", "LastOccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductCanonicalizationIssues_StoreProductId",
                table: "StoreProductCanonicalizationIssues",
                column: "StoreProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreProductCanonicalizationIssues");
        }
    }
}
