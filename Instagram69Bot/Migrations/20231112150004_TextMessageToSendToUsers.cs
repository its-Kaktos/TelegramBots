using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instagram69Bot.Migrations
{
    /// <inheritdoc />
    public partial class TextMessageToSendToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TextMessageToSends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextMessageToSends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsersToSendMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TextMessageToSendId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersToSendMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersToSendMessages_TextMessageToSends_TextMessageToSendId",
                        column: x => x.TextMessageToSendId,
                        principalTable: "TextMessageToSends",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsersToSendMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ChatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersToSendMessages_TextMessageToSendId",
                table: "UsersToSendMessages",
                column: "TextMessageToSendId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersToSendMessages_UserId",
                table: "UsersToSendMessages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsersToSendMessages");

            migrationBuilder.DropTable(
                name: "TextMessageToSends");
        }
    }
}
