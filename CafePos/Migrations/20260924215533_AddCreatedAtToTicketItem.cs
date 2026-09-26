using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafePos.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToTicketItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TicketItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TicketItems");
        }
    }
}
