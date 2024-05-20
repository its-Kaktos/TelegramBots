using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instagram69Bot.Migrations
{
    /// <inheritdoc />
    public partial class Add_UserJoinedChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserJoinedChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    UserChatId = table.Column<long>(type: "bigint", nullable: false),
                    JoinedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserJoinedChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserJoinedChannels_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "ChannelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserJoinedChannels_Users_UserChatId",
                        column: x => x.UserChatId,
                        principalTable: "Users",
                        principalColumn: "ChatId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserJoinedChannels_ChannelId",
                table: "UserJoinedChannels",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserJoinedChannels_UserChatId",
                table: "UserJoinedChannels",
                column: "UserChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserJoinedChannels");
        }
    }
}
