﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OpenTournament.Api.Data;
using OpenTournament.Api.Data.Models;

#nullable disable

namespace OpenTournament.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250507200157_MatchMetadata")]
    partial class MatchMetadata
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Competition", b =>
                {
                    b.Property<string>("CompetitionId")
                        .HasColumnType("varchar(36)");

                    b.Property<int>("CompetitionVisibility")
                        .HasColumnType("integer");

                    b.Property<string>("EventId")
                        .HasColumnType("varchar(36)");

                    b.Property<string>("GameId")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<int>("Mode")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Rules")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("CompetitionId");

                    b.HasIndex("EventId");

                    b.ToTable("Competitions");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Event", b =>
                {
                    b.Property<string>("EventId")
                        .HasColumnType("varchar(36)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("EventState")
                        .HasColumnType("integer");

                    b.Property<int>("EventVisibility")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("EventId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Game", b =>
                {
                    b.Property<string>("GameId")
                        .HasColumnType("varchar(36)");

                    b.Property<string>("Image")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.HasKey("GameId");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Match", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(36)");

                    b.Property<DateTime?>("Completed")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<int>("LocalMatchId")
                        .HasColumnType("integer");

                    b.Property<string>("Participant1Id")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<string>("Participant2Id")
                        .HasColumnType("varchar(36)");

                    b.Property<int>("State")
                        .HasColumnType("integer");

                    b.Property<string>("TournamentId")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<string>("WinnerId")
                        .HasColumnType("text");

                    b.ComplexProperty<Dictionary<string, object>>("Progression", "OpenTournament.Api.Data.Models.Match.Progression#Progression", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<int>("LoseProgressionId")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer")
                                .HasDefaultValue(-1)
                                .HasColumnName("LoseProgressionId");

                            b1.Property<int>("WinProgressionId")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer")
                                .HasDefaultValue(-1)
                                .HasColumnName("WinProgressionId");
                        });

                    b.HasKey("Id");

                    b.HasIndex("Participant1Id");

                    b.HasIndex("Participant2Id");

                    b.HasIndex("TournamentId");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Participant", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(36)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<int>("Rank")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Participants");

                    b.HasData(
                        new
                        {
                            Id = "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF",
                            Name = "Bye",
                            Rank = 2147483647
                        });
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Platform", b =>
                {
                    b.Property<string>("PlatformId")
                        .HasColumnType("varchar(36)");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasColumnType("varchar(128)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(25)");

                    b.HasKey("PlatformId");

                    b.ToTable("Platforms");

                    b.HasData(
                        new
                        {
                            PlatformId = "0193f4b3-4938-79bc-9bf4-a9f1b693730e",
                            ImageUrl = "",
                            Name = "XBox Series X"
                        },
                        new
                        {
                            PlatformId = "0193f4b6-873e-7d40-a6dd-bd898f206abb",
                            ImageUrl = "",
                            Name = "Playstation 5"
                        },
                        new
                        {
                            PlatformId = "0193f4b6-d6b8-7191-a2b8-07cc4c8a86fc",
                            ImageUrl = "",
                            Name = "Nintendo Switch"
                        },
                        new
                        {
                            PlatformId = "0193f4b7-078f-79cd-ba3b-f06d448481f5",
                            ImageUrl = "",
                            Name = "PC"
                        });
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Pool", b =>
                {
                    b.Property<Guid>("PoolId")
                        .HasColumnType("uuid");

                    b.HasKey("PoolId");

                    b.ToTable("Pools");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Registration", b =>
                {
                    b.Property<Guid>("TournamentId")
                        .HasColumnType("uuid");

                    b.Property<string>("ParticipantId")
                        .HasColumnType("varchar(36)");

                    b.HasKey("TournamentId", "ParticipantId");

                    b.HasIndex("ParticipantId");

                    b.ToTable("Registrations");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Stage", b =>
                {
                    b.Property<Guid>("StageId")
                        .HasColumnType("uuid");

                    b.HasKey("StageId");

                    b.ToTable("Stages");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Tournament", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(36)");

                    b.Property<DateTime?>("CompletedOnUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("DrawSize")
                        .HasColumnType("integer");

                    b.Property<int>("EliminationMode")
                        .HasColumnType("integer");

                    b.Property<int>("MaxParticipants")
                        .HasColumnType("integer");

                    b.Property<int>("MinParticipants")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(50)");

                    b.Property<int>("RegistrationMode")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StartedOnUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.ComplexProperty<Dictionary<string, object>>("Creator", "OpenTournament.Api.Data.Models.Tournament.Creator#Creator", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<DateTime>("CreatedOnUtc")
                                .HasColumnType("timestamp with time zone")
                                .HasColumnName("CreatedOnUtc");

                            b1.Property<string>("CreatorId")
                                .IsRequired()
                                .HasColumnType("text")
                                .HasColumnName("CreatorId");
                        });

                    b.HasKey("Id");

                    b.ToTable("Tournaments");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.TournamentMatches", b =>
                {
                    b.Property<Guid>("TournamentId")
                        .HasColumnType("uuid");

                    b.Property<List<MatchMetadata>>("Matches")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("TournamentId");

                    b.ToTable("TournamentMatches");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Competition", b =>
                {
                    b.HasOne("OpenTournament.Api.Data.Models.Event", null)
                        .WithMany("Competitions")
                        .HasForeignKey("EventId");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Match", b =>
                {
                    b.HasOne("OpenTournament.Api.Data.Models.Participant", "Participant1")
                        .WithMany()
                        .HasForeignKey("Participant1Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OpenTournament.Api.Data.Models.Participant", "Participant2")
                        .WithMany()
                        .HasForeignKey("Participant2Id");

                    b.HasOne("OpenTournament.Api.Data.Models.Tournament", null)
                        .WithMany("Matches")
                        .HasForeignKey("TournamentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Participant1");

                    b.Navigation("Participant2");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Registration", b =>
                {
                    b.HasOne("OpenTournament.Api.Data.Models.Participant", "Participant")
                        .WithMany()
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Participant");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Event", b =>
                {
                    b.Navigation("Competitions");
                });

            modelBuilder.Entity("OpenTournament.Api.Data.Models.Tournament", b =>
                {
                    b.Navigation("Matches");
                });
#pragma warning restore 612, 618
        }
    }
}
