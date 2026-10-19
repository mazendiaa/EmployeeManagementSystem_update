using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Controllers;

[Authorize(Roles = AccountRoles.Hr)]
public class DepartmentsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Departments.Include(d => d.Employees).OrderBy(d => d.Name).ToListAsync());
    public IActionResult Create() => View(new Department());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department)
    {
        if (!ModelState.IsValid) return View(department);
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var department = await db.Departments.FindAsync(id);
        return department == null ? NotFound() : View(department);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Department department)
    {
        if (id != department.Id) return NotFound();
        if (!ModelState.IsValid) return View(department);
        db.Departments.Update(department);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var department = await db.Departments.FindAsync(id);
        return department == null ? NotFound() : View(department);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var department = await db.Departments.FindAsync(id);
        if (department == null) return RedirectToAction(nameof(Index));
        db.Departments.Remove(department);
        try { await db.SaveChangesAsync(); TempData["Success"] = "Department deleted."; }
        catch (DbUpdateException) { TempData["Error"] = "This department is assigned to employees and cannot be deleted."; }
        return RedirectToAction(nameof(Index));
    }
}
