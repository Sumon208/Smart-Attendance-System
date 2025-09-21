using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Attendance_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCountedLeaveIdsToSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountedLeaveIds",
                table: "Salaries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountedLeaveIds",
                table: "Salaries");
        }
    }
}
