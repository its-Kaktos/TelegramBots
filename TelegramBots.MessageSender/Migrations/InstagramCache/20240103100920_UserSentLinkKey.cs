#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace TelegramBots.MessageSender.Migrations.InstagramCache
{
    /// <inheritdoc />
    public partial class UserSentLinkKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserSentLinkKey",
                table: "FileCacheInfos",
                type: "nvarchar(750)",
                maxLength: 750,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserSentLinkKey",
                table: "FileCacheInfos");
        }
    }
}