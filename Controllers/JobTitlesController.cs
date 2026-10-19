using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers;

[Authorize(Roles = AccountRoles.Hr)]
public class JobTitlesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.JobTitles.Include(j => j.Employees).OrderBy(j => j.Title).ToListAsync());
    public IActionResult Create() => View(new JobTitle());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobTitle jobTitle)
    {
        if (!ModelState.IsValid) return View(jobTitle);
        db.JobTitles.Add(jobTitle);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var jobTitle = await db.JobTitles.FindAsync(id);
        return jobTitle == null ? NotFound() : View(jobTitle);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobTitle jobTitle)
    {
        if (id != jobTitle.Id) return NotFound();
        if (!ModelState.IsValid) return View(jobTitle);
        db.JobTitles.Update(jobTitle);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var jobTitle = await db.JobTitles.FindAsync(id);
        return jobTitle == null ? NotFound() : View(jobTitle);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var jobTitle = await db.JobTitles.FindAsync(id);
        if (jobTitle == null) return RedirectToAction(nameof(Index));
        db.JobTitles.Remove(jobTitle);
        try { await db.SaveChangesAsync(); TempData["Success"] = "Job title deleted."; }
        catch (DbUpdateException) { TempData["Error"] = "This job title is assigned to employees and cannot be deleted."; }
        return RedirectToAction(nameof(Index));
    }
}
