using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendancePercentage",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "BreakDuration",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IsAbsent",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IsEarlyExit",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IsHalfDay",
                table: "Attendance");

            migrationBuilder.RenameColumn(
                name: "TotalHoursWorked",
                table: "Attendance",
                newName: "TotalWorkingHours");

            migrationBuilder.RenameColumn(
                name: "IsLateEntry",
                table: "Attendance",
                newName: "LateEntryStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalWorkingHours",
                table: "Attendance",
                newName: "TotalHoursWorked");

            migrationBuilder.RenameColumn(
                name: "LateEntryStatus",
                table: "Attendance",
                newName: "IsLateEntry");

            migrationBuilder.AddColumn<double>(
                name: "AttendancePercentage",
                table: "Attendance",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BreakDuration",
                table: "Attendance",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAbsent",
                table: "Attendance",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEarlyExit",
                table: "Attendance",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHalfDay",
                table: "Attendance",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
