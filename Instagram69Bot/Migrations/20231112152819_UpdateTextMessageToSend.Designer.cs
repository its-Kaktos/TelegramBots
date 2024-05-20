﻿// <auto-generated />
using System;
using Instagram69Bot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Instagram69Bot.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20231112152819_UpdateTextMessageToSend")]
    partial class UpdateTextMessageToSend
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Instagram69Bot.Data.Channel", b =>
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

            modelBuilder.Entity("Instagram69Bot.Data.MandatoryChannelsVersion", b =>
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

            modelBuilder.Entity("Instagram69Bot.Data.TextMessageToSend", b =>
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

            modelBuilder.Entity("Instagram69Bot.Data.User", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsInJoinedMandatoryChannels")
                        .HasColumnType("bit");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<int>("VersionUserJoinedId")
                        .HasColumnType("int");

                    b.HasKey("ChatId");

                    b.HasIndex("VersionUserJoinedId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Instagram69Bot.Data.UsersToSendMessage", b =>
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

            modelBuilder.Entity("Instagram69Bot.Data.Channel", b =>
                {
                    b.HasOne("Instagram69Bot.Data.MandatoryChannelsVersion", "Version")
                        .WithMany("Channels")
                        .HasForeignKey("VersionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Version");
                });

            modelBuilder.Entity("Instagram69Bot.Data.User", b =>
                {
                    b.HasOne("Instagram69Bot.Data.MandatoryChannelsVersion", "VersionUserJoined")
                        .WithMany("Users")
                        .HasForeignKey("VersionUserJoinedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("VersionUserJoined");
                });

            modelBuilder.Entity("Instagram69Bot.Data.UsersToSendMessage", b =>
                {
                    b.HasOne("Instagram69Bot.Data.TextMessageToSend", "TextMessageToSend")
                        .WithMany()
                        .HasForeignKey("TextMessageToSendId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Instagram69Bot.Data.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TextMessageToSend");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Instagram69Bot.Data.MandatoryChannelsVersion", b =>
                {
                    b.Navigation("Channels");

                    b.Navigation("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
