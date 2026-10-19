using System.Security.Claims;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers;

[Authorize(Roles = AccountRoles.Hr)]
public class EmployeeChangeRequestsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var requests = await db.EmployeeChangeRequests.Include(r => r.Employee).Include(r => r.ReviewedBy)
            .OrderBy(r => r.Status == EmployeeChangeRequestStatuses.Pending ? 0 : 1)
            .ThenByDescending(r => r.RequestedAtUtc).ToListAsync();
        return View(requests);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, bool approve, string? note)
    {
        var request = await db.EmployeeChangeRequests.Include(r => r.Employee)
            .SingleOrDefaultAsync(r => r.Id == id && r.Status == EmployeeChangeRequestStatuses.Pending);
        if (request == null) return NotFound();

        if (approve)
        {
            var email = request.Email.Trim();
            var phone = NormalizePhone(request.PhoneNumber);
            var otherEmployees = await db.Employees.Where(e => e.Id != request.EmployeeId).Select(e => new { e.Email, e.PhoneNumber }).ToListAsync();
            var duplicateEmployee = otherEmployees.Any(e => e.Email.ToUpper() == email.ToUpper() || NormalizePhone(e.PhoneNumber) == phone);
            var duplicateAccount = await db.UserAccounts.AnyAsync(a => (!a.EmployeeId.HasValue || a.EmployeeId.Value != request.EmployeeId) && a.Email.ToUpper() == email.ToUpper());
            if (duplicateEmployee || duplicateAccount)
            {
                TempData["Error"] = "This request conflicts with an existing employee email or phone number. Decline it and ask the employee to submit corrected details.";
                return RedirectToAction(nameof(Index));
            }

            request.Employee.FullName = request.FullName.Trim();
            request.Employee.Email = email;
            request.Employee.PhoneNumber = request.PhoneNumber.Trim();
            var account = await db.UserAccounts.SingleOrDefaultAsync(a => a.EmployeeId == request.EmployeeId);
            if (account != null)
            {
                account.FullName = request.Employee.FullName;
                account.Email = email;
                if (!string.IsNullOrWhiteSpace(request.ProposedPasswordHash))
                {
                    account.PasswordHash = request.ProposedPasswordHash;
                    account.MustChangePassword = false;
                    var tokens = await db.PasswordResetTokens.Where(t => t.UserAccountId == account.Id).ToListAsync();
                    db.PasswordResetTokens.RemoveRange(tokens);
                }
            }
            request.Status = EmployeeChangeRequestStatuses.Approved;
            request.ReviewNote = NormalizeNote(note);
            TempData["Success"] = "The employee's requested changes have been approved and applied.";
        }
        else
        {
            request.Status = EmployeeChangeRequestStatuses.Rejected;
            request.ProposedPasswordHash = null;
            request.ReviewNote = NormalizeNote(note);
            TempData["Success"] = "The employee's request was declined.";
        }

        request.ReviewedAtUtc = DateTime.UtcNow;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var reviewerId)) request.ReviewedByUserAccountId = reviewerId;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizePhone(string value) => new(value.Where(char.IsDigit).ToArray());
    private static string? NormalizeNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(400, note.Trim().Length)];
}
