using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceAPI.Migrations
{
    /// <inheritdoc />
    public partial class ExtendedAttendanceFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceTime",
                table: "Attendance");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Attendance",
                newName: "AttendanceStatus");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AfternoonCheckIn",
                table: "Attendance",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EveningCheckOut",
                table: "Attendance",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LateEntryStatus",
                table: "Attendance",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchCheckOut",
                table: "Attendance",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MorningCheckIn",
                table: "Attendance",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalWorkingHours",
                table: "Attendance",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AfternoonCheckIn",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "EveningCheckOut",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "LateEntryStatus",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "LunchCheckOut",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "MorningCheckIn",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "TotalWorkingHours",
                table: "Attendance");

            migrationBuilder.RenameColumn(
                name: "AttendanceStatus",
                table: "Attendance",
                newName: "Status");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AttendanceTime",
                table: "Attendance",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
