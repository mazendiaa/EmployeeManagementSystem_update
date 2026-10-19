using System.Security.Claims;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers;

[Authorize(Roles = AccountRoles.Employee)]
public class PortalController(AppDbContext db, IPasswordHasher<UserAccount> hasher) : Controller
{
    public async Task<IActionResult> Profile()
    {
        var employeeId = User.FindFirstValue("EmployeeId");
        if (!int.TryParse(employeeId, out var id)) return View("NoProfile");
        var employee = await db.Employees.Include(e => e.Department).Include(e => e.JobTitle).SingleOrDefaultAsync(e => e.Id == id);
        if (employee == null) return View("NoProfile");
        return View(await BuildSelfProfile(employee, new EmployeeChangeRequestInputModel
        {
            FullName = employee.FullName, Email = employee.Email, PhoneNumber = employee.PhoneNumber
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestProfileChanges([Bind(Prefix = "Request")] EmployeeChangeRequestInputModel model)
    {
        var employeeId = User.FindFirstValue("EmployeeId");
        if (!int.TryParse(employeeId, out var id)) return Forbid();
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == id);
        var account = await db.UserAccounts.SingleOrDefaultAsync(a => a.EmployeeId == id);
        if (employee == null || account == null) return NotFound();
        if (await db.EmployeeChangeRequests.AnyAsync(r => r.EmployeeId == id && r.Status == EmployeeChangeRequestStatuses.Pending))
        {
            TempData["Error"] = "You already have a change request waiting for HR review.";
            return RedirectToAction(nameof(Profile));
        }

        model.FullName = model.FullName.Trim();
        model.Email = model.Email.Trim();
        model.PhoneNumber = model.PhoneNumber.Trim();
        if (account.MustChangePassword && string.IsNullOrWhiteSpace(model.NewPassword))
            ModelState.AddModelError("Request.NewPassword", "Create a new password to replace your temporary sign-in password.");
        if (!string.IsNullOrWhiteSpace(model.NewPassword) && model.NewPassword.Length < 10)
            ModelState.AddModelError("Request.NewPassword", "Use at least 10 characters for a new password.");
        if (!string.IsNullOrWhiteSpace(model.NewPassword) && hasher.VerifyHashedPassword(account, account.PasswordHash, model.NewPassword) != PasswordVerificationResult.Failed)
            ModelState.AddModelError("Request.NewPassword", "Choose a new password different from your current one.");

        var otherEmployees = await db.Employees.Where(e => e.Id != id).Select(e => new { e.Email, e.PhoneNumber }).ToListAsync();
        var emailKey = model.Email.ToUpperInvariant();
        var phoneKey = NormalizePhone(model.PhoneNumber);
        if (otherEmployees.Any(e => e.Email.Trim().ToUpper() == emailKey))
            ModelState.AddModelError("Request.Email", "That email is already assigned to another employee.");
        if (otherEmployees.Any(e => NormalizePhone(e.PhoneNumber) == phoneKey))
            ModelState.AddModelError("Request.PhoneNumber", "That phone number is already assigned to another employee.");
        if (await db.UserAccounts.AnyAsync(a => (!a.EmployeeId.HasValue || a.EmployeeId.Value != id) && a.Email.ToUpper() == emailKey))
            ModelState.AddModelError("Request.Email", "That email is already used by another PeopleDesk account.");

        if (!ModelState.IsValid)
            return View("Profile", await BuildSelfProfile(employee, model));

        var request = new EmployeeChangeRequest
        {
            EmployeeId = id,
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            ProposedPasswordHash = string.IsNullOrWhiteSpace(model.NewPassword) ? null : hasher.HashPassword(account, model.NewPassword)
        };
        db.EmployeeChangeRequests.Add(request);
        await db.SaveChangesAsync();
        TempData["Success"] = "Your profile change request was sent to HR. It will take effect after approval.";
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var employeeId = User.FindFirstValue("EmployeeId");
        if (!int.TryParse(employeeId, out var id)) return View("NoProfile");
        var employee = await db.Employees.Include(e => e.Department).Include(e => e.JobTitle).SingleOrDefaultAsync(e => e.Id == id);
        if (employee == null) return View("NoProfile");
        var selectedMonth = Math.Clamp(month ?? DateTime.Today.Month, 1, 12);
        var selectedYear = Math.Clamp(year ?? DateTime.Today.Year, 2000, DateTime.Today.Year + 1);
        var start = new DateTime(selectedYear, selectedMonth, 1);
        var end = start.AddMonths(1);
        var model = new EmployeePortalViewModel
        {
            Employee = employee,
            Month = selectedMonth,
            Year = selectedYear,
            Attendance = await db.AttendanceRecords.Where(a => a.EmployeeId == id && a.WorkDate >= start && a.WorkDate < end).OrderByDescending(a => a.WorkDate).ToListAsync(),
            Tasks = await db.EmployeeTasks.Where(t => t.EmployeeId == id &&
                    ((t.DueDate >= start && t.DueDate < end) || (t.AssignedAtUtc >= start && t.AssignedAtUtc < end) ||
                     (t.CompletedAtUtc >= start && t.CompletedAtUtc < end)))
                .OrderBy(t => t.Status == EmployeeTaskStatuses.Completed).ThenBy(t => t.DueDate).ToListAsync()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskStatus(int id, string status, int month, int year)
    {
        var employeeId = User.FindFirstValue("EmployeeId");
        if (!int.TryParse(employeeId, out var employeeIdValue)) return Forbid();
        if (status != EmployeeTaskStatuses.InProgress && status != EmployeeTaskStatuses.Completed) return BadRequest();
        var task = await db.EmployeeTasks.SingleOrDefaultAsync(t => t.Id == id && t.EmployeeId == employeeIdValue);
        if (task == null) return NotFound();
        task.Status = status;
        task.CompletedAtUtc = status == EmployeeTaskStatuses.Completed ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        TempData["Success"] = status == EmployeeTaskStatuses.Completed ? "Task marked as completed." : "Task moved back to in progress.";
        return RedirectToAction(nameof(Index), new { month, year });
    }

    private async Task<EmployeeSelfProfileViewModel> BuildSelfProfile(Employee employee, EmployeeChangeRequestInputModel input)
    {
        var history = await db.EmployeeChangeRequests.Where(r => r.EmployeeId == employee.Id)
            .OrderByDescending(r => r.RequestedAtUtc).Take(5).ToListAsync();
        return new EmployeeSelfProfileViewModel
        {
            Employee = employee,
            Request = input,
            PendingRequest = history.FirstOrDefault(r => r.Status == EmployeeChangeRequestStatuses.Pending),
            RecentRequests = history,
            MustChangePassword = await db.UserAccounts.AnyAsync(a => a.EmployeeId == employee.Id && a.MustChangePassword)
        };
    }

    private static string NormalizePhone(string value) => new(value.Where(char.IsDigit).ToArray());
}
