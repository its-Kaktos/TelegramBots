using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instagram69Bot.Migrations
{
    /// <inheritdoc />
    public partial class Add_IsBotBlocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBotBlocked",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBotBlocked",
                table: "Users");
        }
    }
}
