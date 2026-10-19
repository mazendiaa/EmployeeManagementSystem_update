using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers;

[Authorize(Roles = AccountRoles.Hr)]
public class WorkTrackingController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? month, int? year, int? employeeId)
    {
        var selectedMonth = Math.Clamp(month ?? DateTime.Today.Month, 1, 12);
        var selectedYear = Math.Clamp(year ?? DateTime.Today.Year, 2000, DateTime.Today.Year + 1);
        var start = new DateTime(selectedYear, selectedMonth, 1);
        var end = start.AddMonths(1);
        var employees = await db.Employees.OrderBy(e => e.FullName).ToListAsync();
        var attendance = db.AttendanceRecords.Include(a => a.Employee).Where(a => a.WorkDate >= start && a.WorkDate < end);
        var tasks = db.EmployeeTasks.Include(t => t.Employee).Where(t =>
            (t.DueDate >= start && t.DueDate < end) || (t.AssignedAtUtc >= start && t.AssignedAtUtc < end) ||
            (t.CompletedAtUtc >= start && t.CompletedAtUtc < end));
        if (employeeId.HasValue)
        {
            attendance = attendance.Where(a => a.EmployeeId == employeeId.Value);
            tasks = tasks.Where(t => t.EmployeeId == employeeId.Value);
        }
        ViewBag.EmployeeOptions = new SelectList(employees, "Id", "FullName", employeeId);
        var selectedEmployee = employees.FirstOrDefault(e => e.Id == employeeId);
        return View(new WorkTrackingViewModel
        {
            Month = selectedMonth,
            Year = selectedYear,
            EmployeeId = employeeId,
            EmployeeName = selectedEmployee?.FullName,
            Employees = employees,
            Attendance = await attendance.OrderByDescending(a => a.WorkDate).ThenBy(a => a.Employee!.FullName).ToListAsync(),
            Tasks = await tasks.OrderBy(t => t.Status == EmployeeTaskStatuses.Completed).ThenBy(t => t.DueDate).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordAttendance(AttendanceRecord record, int month, int year, int? employeeId)
    {
        record.WorkDate = record.WorkDate.Date;
        if (!await db.Employees.AnyAsync(e => e.Id == record.EmployeeId))
            ModelState.AddModelError(nameof(record.EmployeeId), "Select an employee.");
        if (record.ClockIn.HasValue && record.ClockOut.HasValue && record.ClockOut <= record.ClockIn)
            ModelState.AddModelError(nameof(record.ClockOut), "Clock out must be later than clock in.");
        if (ModelState.IsValid)
        {
            db.AttendanceRecords.Add(record);
            try { await db.SaveChangesAsync(); TempData["Success"] = "Attendance record added."; }
            catch (DbUpdateException) { TempData["Error"] = "An attendance record already exists for this employee and date."; }
        }
        else TempData["Error"] = "Check the employee, date, and clock times, then try again.";
        return RedirectToAction(nameof(Index), new { month, year, employeeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttendance(int id, DateTime workDate, TimeSpan? clockIn, TimeSpan? clockOut, string? notes, int month, int year, int? employeeId)
    {
        if (clockIn.HasValue && clockOut.HasValue && clockOut <= clockIn)
        {
            TempData["Error"] = "Clock out must be later than clock in.";
            return RedirectToAction(nameof(Index), new { month, year, employeeId });
        }
        var record = await db.AttendanceRecords.FindAsync(id);
        if (record == null) return NotFound();
        record.WorkDate = workDate.Date;
        record.ClockIn = clockIn;
        record.ClockOut = clockOut;
        record.Notes = notes;
        try { await db.SaveChangesAsync(); TempData["Success"] = "Attendance record updated."; }
        catch (DbUpdateException) { TempData["Error"] = "A record already exists for that employee and date."; }
        return RedirectToAction(nameof(Index), new { month, year, employeeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttendance(int id, int month, int year, int? employeeId)
    {
        var record = await db.AttendanceRecords.FindAsync(id);
        if (record != null) { db.AttendanceRecords.Remove(record); await db.SaveChangesAsync(); TempData["Success"] = "Attendance record removed."; }
        return RedirectToAction(nameof(Index), new { month, year, employeeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTask(EmployeeTask task, int month, int year, int? employeeId)
    {
        task.Status = EmployeeTaskStatuses.Assigned;
        task.AssignedAtUtc = DateTime.UtcNow;
        task.CompletedAtUtc = null;
        if (!await db.Employees.AnyAsync(e => e.Id == task.EmployeeId)) ModelState.AddModelError(nameof(task.EmployeeId), "Select an employee.");
        if (ModelState.IsValid)
        {
            db.EmployeeTasks.Add(task);
            await db.SaveChangesAsync();
            TempData["Success"] = "Task assigned to the employee.";
        }
        else TempData["Error"] = "Add a task title and choose an employee.";
        return RedirectToAction(nameof(Index), new { month, year, employeeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTask(int id, string title, string? description, DateTime? dueDate, string status, int month, int year, int? employeeId)
    {
        if (status != EmployeeTaskStatuses.Assigned && status != EmployeeTaskStatuses.InProgress && status != EmployeeTaskStatuses.Completed)
            return BadRequest();
        var task = await db.EmployeeTasks.FindAsync(id);
        if (task == null) return NotFound();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 140)
        {
            TempData["Error"] = "Task title must have 1 to 140 characters.";
            return RedirectToAction(nameof(Index), new { month, year, employeeId });
        }
        if (description?.Length > 1000)
        {
            TempData["Error"] = "Task description must be 1,000 characters or fewer.";
            return RedirectToAction(nameof(Index), new { month, year, employeeId });
        }
        task.Title = title.Trim();
        task.Description = description;
        task.DueDate = dueDate?.Date;
        task.Status = status;
        task.CompletedAtUtc = status == EmployeeTaskStatuses.Completed ? task.CompletedAtUtc ?? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Task updated.";
        return RedirectToAction(nameof(Index), new { month, year, employeeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(int id, int month, int year, int? employeeId)
    {
        var task = await db.EmployeeTasks.FindAsync(id);
        if (task != null) { db.EmployeeTasks.Remove(task); await db.SaveChangesAsync(); TempData["Success"] = "Task removed."; }
        return RedirectToAction(nameof(Index), new { month, year, employeeId });
    }
}
