using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmaciaSmart.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Trigger = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    RecommendationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Warning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Restriction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PharmacistApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationRules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationRules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationRules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    RequestText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ReviewStatus = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Recommendations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recommendations_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recommendations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    RecommendationRuleId = table.Column<int>(type: "int", nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Warning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationItems_RecommendationRules_RecommendationRuleId",
                        column: x => x.RecommendationRuleId,
                        principalTable: "RecommendationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecommendationItems_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationItems_ProductId",
                table: "RecommendationItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationItems_RecommendationId",
                table: "RecommendationItems",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationItems_RecommendationRuleId",
                table: "RecommendationItems",
                column: "RecommendationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRules_CategoryId",
                table: "RecommendationRules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRules_CreatedByUserId",
                table: "RecommendationRules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationRules_ProductId",
                table: "RecommendationRules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_BranchId",
                table: "Recommendations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_CategoryId",
                table: "Recommendations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_CreatedAt",
                table: "Recommendations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_CustomerId",
                table: "Recommendations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_ReviewedByUserId",
                table: "Recommendations",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationItems");

            migrationBuilder.DropTable(
                name: "RecommendationRules");

            migrationBuilder.DropTable(
                name: "Recommendations");
        }
    }
}
