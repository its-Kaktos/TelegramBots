using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Youtube69bot.Migrations
{
    /// <inheritdoc />
    public partial class DurationInSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationInSeconds",
                table: "ResolvedYoutubeLinks",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationInSeconds",
                table: "ResolvedYoutubeLinks");
        }
    }
}
