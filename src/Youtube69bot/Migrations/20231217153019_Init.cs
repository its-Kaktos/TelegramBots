using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Youtube69bot.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MandatoryChannelsVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryChannelsVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TextMessageToSends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextMessageToSends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChannelJoinLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsNotAllowedToLeaveChannel = table.Column<bool>(type: "bit", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    UsersJoinedFromBot = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.ChannelId);
                    table.ForeignKey(
                        name: "FK_Channels_MandatoryChannelsVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "MandatoryChannelsVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsInJoinedMandatoryChannels = table.Column<bool>(type: "bit", nullable: false),
                    JoinedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VersionUserJoinedId = table.Column<int>(type: "int", nullable: false),
                    IsBotBlocked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ChatId);
                    table.ForeignKey(
                        name: "FK_Users_MandatoryChannelsVersions_VersionUserJoinedId",
                        column: x => x.VersionUserJoinedId,
                        principalTable: "MandatoryChannelsVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    DateEventHappened = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ChatId",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_Channels_VersionId",
                table: "Channels",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEvents_UserId",
                table: "UserEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserJoinedChannels_ChannelId",
                table: "UserJoinedChannels",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserJoinedChannels_UserChatId",
                table: "UserJoinedChannels",
                column: "UserChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_VersionUserJoinedId",
                table: "Users",
                column: "VersionUserJoinedId");

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
                name: "UserEvents");

            migrationBuilder.DropTable(
                name: "UserJoinedChannels");

            migrationBuilder.DropTable(
                name: "UsersToSendMessages");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "TextMessageToSends");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MandatoryChannelsVersions");
        }
    }
}
