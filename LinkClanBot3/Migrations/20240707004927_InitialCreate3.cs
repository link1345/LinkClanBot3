using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkClanBot3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnteringRoom",
                table: "MemberTimeLine",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventDate",
                table: "MemberTimeLine",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "after_channel_id",
                table: "MemberTimeLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "after_channel_name",
                table: "MemberTimeLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "before_channel_id",
                table: "MemberTimeLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "before_channel_name",
                table: "MemberTimeLine",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnteringRoom",
                table: "MemberTimeLine");

            migrationBuilder.DropColumn(
                name: "EventDate",
                table: "MemberTimeLine");

            migrationBuilder.DropColumn(
                name: "after_channel_id",
                table: "MemberTimeLine");

            migrationBuilder.DropColumn(
                name: "after_channel_name",
                table: "MemberTimeLine");

            migrationBuilder.DropColumn(
                name: "before_channel_id",
                table: "MemberTimeLine");

            migrationBuilder.DropColumn(
                name: "before_channel_name",
                table: "MemberTimeLine");
        }
    }
}
