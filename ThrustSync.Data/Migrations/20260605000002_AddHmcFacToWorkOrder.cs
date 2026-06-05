using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrustSync.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHmcFacToWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FAC",
                table: "WorkOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HMC",
                table: "WorkOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FAC",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "HMC",
                table: "WorkOrders");
        }
    }
}
