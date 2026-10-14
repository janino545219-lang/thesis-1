using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmaciaSmart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackAndInteraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendationFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationItemId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationFeedbacks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationFeedbacks_RecommendationItems_RecommendationItemId",
                        column: x => x.RecommendationItemId,
                        principalTable: "RecommendationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    RecommendationId = table.Column<int>(type: "int", nullable: false),
                    InteractionType = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationInteractions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationInteractions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecommendationInteractions_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationFeedbacks_CustomerId",
                table: "RecommendationFeedbacks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationFeedbacks_RecommendationItemId",
                table: "RecommendationFeedbacks",
                column: "RecommendationItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationInteractions_CustomerId",
                table: "RecommendationInteractions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationInteractions_ProductId",
                table: "RecommendationInteractions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationInteractions_RecommendationId_InteractionType",
                table: "RecommendationInteractions",
                columns: new[] { "RecommendationId", "InteractionType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationFeedbacks");

            migrationBuilder.DropTable(
                name: "RecommendationInteractions");
        }
    }
}
