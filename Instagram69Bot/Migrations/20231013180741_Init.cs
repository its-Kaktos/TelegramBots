using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instagram69Bot.Migrations
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
                    Step = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsInJoinedMandatoryChannels = table.Column<bool>(type: "bit", nullable: false),
                    VersionUserJoinedId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Channels_VersionId",
                table: "Channels",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_VersionUserJoinedId",
                table: "Users",
                column: "VersionUserJoinedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "MandatoryChannelsVersions");
        }
    }
}
