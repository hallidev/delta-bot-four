using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeltaBotFour.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Db4States",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastActivityTimeUtcKey = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastProcessedCommentIds = table.Column<string>(type: "TEXT", nullable: true),
                    LastProcessedEditIds = table.Column<string>(type: "TEXT", nullable: true),
                    IgnoreQuotedDeltaPMUserList = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Db4States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deltaboards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DeltaboardType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deltaboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeltaComments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsEdited = table.Column<bool>(type: "INTEGER", nullable: false),
                    FromUsername = table.Column<string>(type: "TEXT", nullable: true),
                    ToUsername = table.Column<string>(type: "TEXT", nullable: true),
                    CommentText = table.Column<string>(type: "TEXT", nullable: true),
                    LinkId = table.Column<string>(type: "TEXT", nullable: true),
                    Permalink = table.Column<string>(type: "TEXT", nullable: true),
                    Shortlink = table.Column<string>(type: "TEXT", nullable: true),
                    ParentPostId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentPostLinkId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentPostPermalink = table.Column<string>(type: "TEXT", nullable: true),
                    ParentPostShortlink = table.Column<string>(type: "TEXT", nullable: true),
                    ParentPostTitle = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeltaComments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeltaLogPostMappings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MainSubPostUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DeltaLogPostId = table.Column<string>(type: "TEXT", nullable: true),
                    DeltaLogPostUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeltaLogPostMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeltaboardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeltaboardId = table.Column<string>(type: "TEXT", nullable: true),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeltaboardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeltaboardEntries_Deltaboards_DeltaboardId",
                        column: x => x.DeltaboardId,
                        principalTable: "Deltaboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeltaboardEntries_DeltaboardId",
                table: "DeltaboardEntries",
                column: "DeltaboardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Db4States");

            migrationBuilder.DropTable(
                name: "DeltaboardEntries");

            migrationBuilder.DropTable(
                name: "DeltaComments");

            migrationBuilder.DropTable(
                name: "DeltaLogPostMappings");

            migrationBuilder.DropTable(
                name: "Deltaboards");
        }
    }
}
