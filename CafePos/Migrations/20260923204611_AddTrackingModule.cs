using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafePos.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "RoundingAmount",
                table: "Tickets",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "RoundingUserId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "TicketItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "TicketItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "TicketActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    TicketItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    AppUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    ReasonCode = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketActionLogs_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketActionLogs_TicketItems_TicketItemId",
                        column: x => x.TicketItemId,
                        principalTable: "TicketItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketActionLogs_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RoundingUserId",
                table: "Tickets",
                column: "RoundingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketActionLogs_AppUserId",
                table: "TicketActionLogs",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketActionLogs_TicketId",
                table: "TicketActionLogs",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketActionLogs_TicketItemId",
                table: "TicketActionLogs",
                column: "TicketItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AppUsers_RoundingUserId",
                table: "Tickets",
                column: "RoundingUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AppUsers_RoundingUserId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "TicketActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_RoundingUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RoundingUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "TicketItems");

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "TicketItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "RoundingAmount",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
