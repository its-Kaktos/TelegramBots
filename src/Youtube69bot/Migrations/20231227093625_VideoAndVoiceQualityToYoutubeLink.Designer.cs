﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Youtube69bot.Data;

#nullable disable

namespace Youtube69bot.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20231227093625_VideoAndVoiceQualityToYoutubeLink")]
    partial class VideoAndVoiceQualityToYoutubeLink
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Youtube69bot.Data.Channel", b =>
                {
                    b.Property<long>("ChannelId")
                        .HasColumnType("bigint");

                    b.Property<string>("ChannelJoinLink")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsNotAllowedToLeaveChannel")
                        .HasColumnType("bit");

                    b.Property<int>("UsersJoinedFromBot")
                        .HasColumnType("int");

                    b.Property<int>("VersionId")
                        .HasColumnType("int");

                    b.HasKey("ChannelId");

                    b.HasIndex("VersionId");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("Youtube69bot.Data.MandatoryChannelsVersion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("AddedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Version")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("MandatoryChannelsVersions");
                });

            modelBuilder.Entity("Youtube69bot.Data.ResolvedYoutubeLink", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTimeOffset>("AddedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<float?>("AudioQuality")
                        .HasColumnType("real");

                    b.Property<string>("DownloadLink")
                        .IsRequired()
                        .HasMaxLength(1500)
                        .HasColumnType("nvarchar(1500)");

                    b.Property<string>("ReplyMessageId")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.Property<long>("TelegramChatId")
                        .HasColumnType("bigint");

                    b.Property<int>("TelegramMessageId")
                        .HasColumnType("int");

                    b.Property<string>("ThumbnailLink")
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Title")
                        .HasMaxLength(750)
                        .HasColumnType("nvarchar(750)");

                    b.Property<int?>("VideoHeight")
                        .HasColumnType("int");

                    b.Property<int?>("VideoWidth")
                        .HasColumnType("int");

                    b.Property<string>("YoutubeLink")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("ResolvedYoutubeLinks");
                });

            modelBuilder.Entity("Youtube69bot.Data.TextMessageToSend", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsCompleted")
                        .HasColumnType("bit");

                    b.Property<string>("MessageText")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("TextMessageToSends");
                });

            modelBuilder.Entity("Youtube69bot.Data.User", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsBotBlocked")
                        .HasColumnType("bit");

                    b.Property<bool>("IsInJoinedMandatoryChannels")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("JoinedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<int>("VersionUserJoinedId")
                        .HasColumnType("int");

                    b.HasKey("ChatId");

                    b.HasIndex("VersionUserJoinedId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Youtube69bot.Data.UserEvent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("DateEventHappened")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("EventType")
                        .HasColumnType("int");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserEvents");
                });

            modelBuilder.Entity("Youtube69bot.Data.UserJoinedChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<long>("ChannelId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("JoinedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("UserChatId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("UserChatId");

                    b.ToTable("UserJoinedChannels");
                });

            modelBuilder.Entity("Youtube69bot.Data.UsersToSendMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("TextMessageToSendId")
                        .HasColumnType("int");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("TextMessageToSendId");

                    b.HasIndex("UserId");

                    b.ToTable("UsersToSendMessages");
                });

            modelBuilder.Entity("Youtube69bot.Data.Channel", b =>
                {
                    b.HasOne("Youtube69bot.Data.MandatoryChannelsVersion", "Version")
                        .WithMany("Channels")
                        .HasForeignKey("VersionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Version");
                });

            modelBuilder.Entity("Youtube69bot.Data.User", b =>
                {
                    b.HasOne("Youtube69bot.Data.MandatoryChannelsVersion", "VersionUserJoined")
                        .WithMany("Users")
                        .HasForeignKey("VersionUserJoinedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("VersionUserJoined");
                });

            modelBuilder.Entity("Youtube69bot.Data.UserEvent", b =>
                {
                    b.HasOne("Youtube69bot.Data.User", "User")
                        .WithMany("UserEvents")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Youtube69bot.Data.UserJoinedChannel", b =>
                {
                    b.HasOne("Youtube69bot.Data.Channel", "Channel")
                        .WithMany("UserJoinedChannels")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Youtube69bot.Data.User", "UserChat")
                        .WithMany("UserJoinedChannels")
                        .HasForeignKey("UserChatId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Channel");

                    b.Navigation("UserChat");
                });

            modelBuilder.Entity("Youtube69bot.Data.UsersToSendMessage", b =>
                {
                    b.HasOne("Youtube69bot.Data.TextMessageToSend", "TextMessageToSend")
                        .WithMany()
                        .HasForeignKey("TextMessageToSendId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Youtube69bot.Data.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TextMessageToSend");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Youtube69bot.Data.Channel", b =>
                {
                    b.Navigation("UserJoinedChannels");
                });

            modelBuilder.Entity("Youtube69bot.Data.MandatoryChannelsVersion", b =>
                {
                    b.Navigation("Channels");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("Youtube69bot.Data.User", b =>
                {
                    b.Navigation("UserEvents");

                    b.Navigation("UserJoinedChannels");
                });
#pragma warning restore 612, 618
        }
    }
}
