using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Youtube69bot.Migrations
{
    /// <inheritdoc />
    public partial class VideoAndVoiceQualityToYoutubeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "AudioQuality",
                table: "ResolvedYoutubeLinks",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoHeight",
                table: "ResolvedYoutubeLinks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoWidth",
                table: "ResolvedYoutubeLinks",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioQuality",
                table: "ResolvedYoutubeLinks");

            migrationBuilder.DropColumn(
                name: "VideoHeight",
                table: "ResolvedYoutubeLinks");

            migrationBuilder.DropColumn(
                name: "VideoWidth",
                table: "ResolvedYoutubeLinks");
        }
    }
}
