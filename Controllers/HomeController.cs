using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeManagementSystem.Controllers;

[Authorize(Roles = AccountRoles.Hr)]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context) => _context = context;

    public IActionResult Index()
    {
        ViewBag.EmployeeCount = _context.Employees.Count();
        ViewBag.DepartmentCount = _context.Departments.Count();
        ViewBag.JobTitleCount = _context.JobTitles.Count();
        ViewBag.PendingProfileRequests = _context.EmployeeChangeRequests.Count(r => r.Status == EmployeeChangeRequestStatuses.Pending);
        ViewBag.NewestEmployees = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .OrderByDescending(e => e.Id)
            .Take(5)
            .ToList();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
