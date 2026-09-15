using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAlreadyDidIt.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HandoffListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "authorship_attested_at",
                table: "apps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handoff_note",
                table: "apps",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "handoff_requested_at",
                table: "apps",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "authorship_attested_at",
                table: "apps");

            migrationBuilder.DropColumn(
                name: "handoff_note",
                table: "apps");

            migrationBuilder.DropColumn(
                name: "handoff_requested_at",
                table: "apps");
        }
    }
}
