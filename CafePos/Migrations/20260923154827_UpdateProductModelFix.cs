using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafePos.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductModelFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStockTracked",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStockTracked",
                table: "Products");
        }
    }
}
