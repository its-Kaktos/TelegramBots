#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace TelegramBots.MessageSender.Migrations.YoutubeCache
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileCacheInfos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserSentLink = table.Column<string>(type: "nvarchar(750)", maxLength: 750, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_FileCacheInfos", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "AudioCaches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileCacheInfoId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(750)", maxLength: 750, nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(750)", maxLength: 750, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Quality = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioCaches_FileCacheInfos_FileCacheInfoId",
                        column: x => x.FileCacheInfoId,
                        principalTable: "FileCacheInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoCaches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileCacheInfoId = table.Column<long>(type: "bigint", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(750)", maxLength: 750, nullable: true),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoCaches_FileCacheInfos_FileCacheInfoId",
                        column: x => x.FileCacheInfoId,
                        principalTable: "FileCacheInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioCaches_FileCacheInfoId",
                table: "AudioCaches",
                column: "FileCacheInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoCaches_FileCacheInfoId",
                table: "VideoCaches",
                column: "FileCacheInfoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioCaches");

            migrationBuilder.DropTable(
                name: "VideoCaches");

            migrationBuilder.DropTable(
                name: "FileCacheInfos");
        }
    }
}