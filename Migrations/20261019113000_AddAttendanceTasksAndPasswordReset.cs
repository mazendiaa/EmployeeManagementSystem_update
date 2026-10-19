using EmployeeManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagementSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261019113000_AddAttendanceTasksAndPasswordReset")]
public partial class AddAttendanceTasksAndPasswordReset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "MustChangePassword", table: "UserAccounts", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.DropForeignKey("FK_UserAccounts_Employees_EmployeeId", "UserAccounts");
        migrationBuilder.AddForeignKey(
            name: "FK_UserAccounts_Employees_EmployeeId",
            table: "UserAccounts",
            column: "EmployeeId",
            principalTable: "Employees",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "AttendanceRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                EmployeeId = table.Column<int>(type: "int", nullable: false),
                WorkDate = table.Column<DateTime>(type: "date", nullable: false),
                ClockIn = table.Column<TimeSpan>(type: "time", nullable: true),
                ClockOut = table.Column<TimeSpan>(type: "time", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                table.ForeignKey("FK_AttendanceRecords_Employees_EmployeeId", x => x.EmployeeId, "Employees", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EmployeeTasks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                EmployeeId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                DueDate = table.Column<DateTime>(type: "date", nullable: true),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeTasks", x => x.Id);
                table.ForeignKey("FK_EmployeeTasks_Employees_EmployeeId", x => x.EmployeeId, "Employees", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PasswordResetTokens",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserAccountId = table.Column<int>(type: "int", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                table.ForeignKey("FK_PasswordResetTokens_UserAccounts_UserAccountId", x => x.UserAccountId, "UserAccounts", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AttendanceRecords_EmployeeId_WorkDate", "AttendanceRecords", new[] { "EmployeeId", "WorkDate" }, unique: true);
        migrationBuilder.CreateIndex("IX_EmployeeTasks_EmployeeId", "EmployeeTasks", "EmployeeId");
        migrationBuilder.CreateIndex("IX_PasswordResetTokens_TokenHash", "PasswordResetTokens", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_PasswordResetTokens_UserAccountId", "PasswordResetTokens", "UserAccountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AttendanceRecords");
        migrationBuilder.DropTable("EmployeeTasks");
        migrationBuilder.DropTable("PasswordResetTokens");
        migrationBuilder.DropColumn("MustChangePassword", "UserAccounts");
        migrationBuilder.DropForeignKey("FK_UserAccounts_Employees_EmployeeId", "UserAccounts");
        migrationBuilder.AddForeignKey("FK_UserAccounts_Employees_EmployeeId", "UserAccounts", "EmployeeId", "Employees", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }
}
