using EmployeeManagementSystem.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagementSystem.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261019143000_AddEmployeeChangeRequests")]
public partial class AddEmployeeChangeRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmployeeChangeRequests",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                EmployeeId = table.Column<int>(type: "int", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                ProposedPasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReviewedByUserAccountId = table.Column<int>(type: "int", nullable: true),
                ReviewNote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeChangeRequests", x => x.Id);
                table.ForeignKey("FK_EmployeeChangeRequests_Employees_EmployeeId", x => x.EmployeeId, "Employees", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_EmployeeChangeRequests_UserAccounts_ReviewedByUserAccountId", x => x.ReviewedByUserAccountId, "UserAccounts", "Id", onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.CreateIndex(name: "IX_EmployeeChangeRequests_EmployeeId", table: "EmployeeChangeRequests", column: "EmployeeId", unique: true, filter: "[Status] = N'Pending'");
        migrationBuilder.CreateIndex(name: "IX_EmployeeChangeRequests_EmployeeId_RequestedAtUtc", table: "EmployeeChangeRequests", columns: new[] { "EmployeeId", "RequestedAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_EmployeeChangeRequests_ReviewedByUserAccountId", table: "EmployeeChangeRequests", column: "ReviewedByUserAccountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "EmployeeChangeRequests");
}
