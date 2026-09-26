using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafePos.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserAndPinTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "AppUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "AppUsers",
                newName: "PinCode");

            migrationBuilder.AddColumn<int>(
                name: "AppUserId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "AppUsers",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AppUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AppUserId",
                table: "Tickets",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AppUsers_AppUserId",
                table: "Tickets",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AppUsers_AppUserId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AppUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppUsers");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AppUsers",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "PinCode",
                table: "AppUsers",
                newName: "PasswordHash");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "AppUsers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
