using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Youtube69bot.Migrations
{
    /// <inheritdoc />
    public partial class ResolvedYoutubeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResolvedYoutubeLinks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    YoutubeLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramMessageId = table.Column<int>(type: "int", nullable: false),
                    ReplyMessageId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AddedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DownloadLink = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    ThumbnailLink = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(750)", maxLength: 750, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResolvedYoutubeLinks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResolvedYoutubeLinks");
        }
    }
}
