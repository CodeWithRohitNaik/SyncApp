using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrustSync.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUniqueIdentifierToFRACPR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_FRACPR",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_JCN",
                table: "WorkOrders");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_FRACPR",
                table: "WorkOrders",
                column: "FRACPR",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_JCN",
                table: "WorkOrders",
                column: "JCN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_FRACPR",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_JCN",
                table: "WorkOrders");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_FRACPR",
                table: "WorkOrders",
                column: "FRACPR");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_JCN",
                table: "WorkOrders",
                column: "JCN",
                unique: true);
        }
    }
}
