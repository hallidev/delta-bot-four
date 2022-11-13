using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeltaBotFour.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropRank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rank",
                table: "DeltaboardEntries");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedUtc",
                table: "DeltaboardEntries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedUtc",
                table: "DeltaboardEntries");

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                table: "DeltaboardEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
