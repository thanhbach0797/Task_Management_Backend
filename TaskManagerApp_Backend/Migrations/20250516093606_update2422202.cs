using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagerApp_Backend.Migrations
{
    /// <inheritdoc />
    public partial class update2422202 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Employees",
                newName: "Username");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Employees",
                newName: "Name");
        }
    }
}
