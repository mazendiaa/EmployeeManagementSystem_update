using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeManagementSystem.Controllers;

[Authorize(Roles = AccountRoles.Hr)]
public class EmployeesController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public EmployeesController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? searchText, int? departmentId, int? jobTitleId, int page = 1)
    {
        int pageSize = 5;

        var employees = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            employees = employees.Where(e => e.FullName.Contains(searchText) || e.Email.Contains(searchText));
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            employees = employees.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (jobTitleId.HasValue && jobTitleId.Value > 0)
        {
            employees = employees.Where(e => e.JobTitleId == jobTitleId.Value);
        }

        int totalEmployees = await employees.CountAsync();
        int totalPages = (int)Math.Ceiling(totalEmployees / (double)pageSize);

        if (page < 1)
        {
            page = 1;
        }

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var employeeList = await employees
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchText = searchText;
        ViewBag.DepartmentId = departmentId;
        ViewBag.JobTitleId = jobTitleId;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        await LoadDropdowns();

        return View(employeeList);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound();
        }

        ViewBag.UserAccount = await _context.UserAccounts.SingleOrDefaultAsync(a => a.EmployeeId == employee.Id);
        ViewBag.AttendanceCount = await _context.AttendanceRecords.CountAsync(a => a.EmployeeId == employee.Id);
        ViewBag.TaskCount = await _context.EmployeeTasks.CountAsync(t => t.EmployeeId == employee.Id);
        ViewBag.NewTemporaryPassword = TempData["NewTemporaryPassword"];
        ViewBag.TemporaryPasswordEmployee = TempData["TemporaryPasswordEmployee"];
        return View(employee);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new Employee());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee employee, IFormFile? profileImage)
    {
        employee.Email = employee.Email?.Trim() ?? string.Empty;
        employee.PhoneNumber = employee.PhoneNumber?.Trim() ?? string.Empty;
        await ValidateImage(profileImage);
        await ValidateUniqueContactDetails(employee);
        await ValidateUniqueLoginEmail(employee.Email);
        await ValidateRequiredHrFields(employee);

        if (ModelState.IsValid)
        {
            employee.ProfileImagePath = await SaveImage(profileImage);
            _context.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await LoadDropdowns();
        return View(employee);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        await LoadDropdowns();
        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee employee, IFormFile? profileImage, bool removeProfileImage = false)
    {
        if (id != employee.Id)
        {
            return NotFound();
        }

        employee.Email = employee.Email?.Trim() ?? string.Empty;
        employee.PhoneNumber = employee.PhoneNumber?.Trim() ?? string.Empty;
        await ValidateImage(profileImage);
        await ValidateUniqueContactDetails(employee, id);
        await ValidateUniqueLoginEmail(employee.Email, id);
        await ValidateRequiredHrFields(employee);

        if (ModelState.IsValid)
        {
            var oldEmployee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (oldEmployee == null)
            {
                return NotFound();
            }

            var previousImagePath = oldEmployee.ProfileImagePath;
            var linkedAccount = await _context.UserAccounts.FirstOrDefaultAsync(a => a.EmployeeId == id);
            if (linkedAccount != null)
            {
                linkedAccount.Email = employee.Email;
                linkedAccount.FullName = employee.FullName;
            }

            if (profileImage != null && profileImage.Length > 0)
            {
                employee.ProfileImagePath = await SaveImage(profileImage);
            }
            else if (removeProfileImage)
            {
                employee.ProfileImagePath = null;
            }
            else
            {
                employee.ProfileImagePath = previousImagePath;
            }

            _context.Update(employee);
            await _context.SaveChangesAsync();

            if ((removeProfileImage || (profileImage != null && profileImage.Length > 0)) &&
                !string.IsNullOrEmpty(previousImagePath) && previousImagePath != employee.ProfileImagePath)
            {
                DeleteStoredImage(previousImagePath);
            }

            return RedirectToAction(nameof(Index));
        }

        await LoadDropdowns();
        return View(employee);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            DeleteStoredImage(employee.ProfileImagePath);
            TempData["Success"] = "The employee, linked login, attendance, and tasks were removed.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.JobTitles = new SelectList(await _context.JobTitles.OrderBy(j => j.Title).ToListAsync(), "Id", "Title");
    }

    private async Task ValidateUniqueContactDetails(Employee employee, int? excludedEmployeeId = null)
    {
        var existingContacts = await _context.Employees
            .Where(e => !excludedEmployeeId.HasValue || e.Id != excludedEmployeeId.Value)
            .Select(e => new { e.Email, e.PhoneNumber })
            .ToListAsync();

        var normalizedEmail = NormalizeEmail(employee.Email);
        if (existingContacts.Any(e => NormalizeEmail(e.Email) == normalizedEmail))
        {
            ModelState.AddModelError(nameof(employee.Email), "This email is already assigned to another employee.");
        }

        var normalizedPhone = NormalizePhone(employee.PhoneNumber);
        if (existingContacts.Any(e => NormalizePhone(e.PhoneNumber) == normalizedPhone))
        {
            ModelState.AddModelError(nameof(employee.PhoneNumber), "This phone number is already assigned to another employee.");
        }
    }

    private async Task ValidateUniqueLoginEmail(string email, int? excludedEmployeeId = null)
    {
        var accounts = _context.UserAccounts.Where(a => a.Email.ToUpper() == email.ToUpper());
        if (excludedEmployeeId.HasValue) accounts = accounts.Where(a => a.EmployeeId != excludedEmployeeId.Value);
        if (await accounts.AnyAsync())
            ModelState.AddModelError(nameof(Employee.Email), "This email is already used by a PeopleDesk login account.");
    }

    private async Task ValidateRequiredHrFields(Employee employee)
    {
        if (!employee.HireDate.HasValue)
            ModelState.AddModelError(nameof(Employee.HireDate), "Enter the employee's hire date.");

        if (!employee.Salary.HasValue || employee.Salary < 1 || employee.Salary > 1000000)
            ModelState.AddModelError(nameof(Employee.Salary), "Enter a salary between 1 and 1,000,000.");

        if (!employee.DepartmentId.HasValue || !await _context.Departments.AnyAsync(d => d.Id == employee.DepartmentId.Value))
            ModelState.AddModelError(nameof(Employee.DepartmentId), "Select a valid department.");

        if (!employee.JobTitleId.HasValue || !await _context.JobTitles.AnyAsync(j => j.Id == employee.JobTitleId.Value))
            ModelState.AddModelError(nameof(Employee.JobTitleId), "Select a valid job title.");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static string NormalizePhone(string phoneNumber) =>
        new(phoneNumber.Where(char.IsDigit).ToArray());

    private async Task ValidateImage(IFormFile? image)
    {
        if (image == null || image.Length == 0)
        {
            return;
        }

        string extension = Path.GetExtension(image.FileName).ToLower();
        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError("ProfileImagePath", "Only JPG, JPEG, PNG, and GIF images are allowed.");
        }

        if (!image.ContentType.StartsWith("image/"))
        {
            ModelState.AddModelError("ProfileImagePath", "The uploaded file must be an image.");
        }

        if (image.Length > 2 * 1024 * 1024)
        {
            ModelState.AddModelError("ProfileImagePath", "Image size must be 2 MB or less.");
        }

        await Task.CompletedTask;
    }

    private async Task<string?> SaveImage(IFormFile? image)
    {
        if (image == null || image.Length == 0)
        {
            return null;
        }

        string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "employees");
        Directory.CreateDirectory(uploadsFolder);

        string extension = Path.GetExtension(image.FileName).ToLower();
        string fileName = Guid.NewGuid().ToString() + extension;
        string filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        return "/uploads/employees/" + fileName;
    }

    private void DeleteStoredImage(string? imagePath)
    {
        const string uploadPrefix = "/uploads/employees/";
        if (string.IsNullOrWhiteSpace(imagePath) || !imagePath.StartsWith(uploadPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileName = Path.GetFileName(imagePath[uploadPrefix.Length..]);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var uploadsFolder = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads", "employees"));
        var filePath = Path.GetFullPath(Path.Combine(uploadsFolder, fileName));
        if (filePath.StartsWith(uploadsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }
}
