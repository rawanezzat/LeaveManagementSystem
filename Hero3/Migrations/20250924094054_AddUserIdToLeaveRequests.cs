using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hero3.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToLeaveRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LeaveRequests",
                newName: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "LeaveRequests",
                newName: "Id");
        }
    }
}
