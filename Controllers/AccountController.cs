using System.Security.Claims;
using System.Security.Cryptography;
using System.Data;
using System.Text;
using System.Text.Encodings.Web;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using EmployeeManagementSystem.Services;

namespace EmployeeManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<UserAccount> _hasher;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext db, IPasswordHasher<UserAccount> hasher, IEmailSender emailSender, ILogger<AccountController> logger)
    {
        _db = db;
        _hasher = hasher;
        _emailSender = emailSender;
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Login(string? role = null, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectByRole();
        ViewBag.ReturnUrl = returnUrl;
        var selectedRole = role == AccountRoles.Employee ? AccountRoles.Employee : AccountRoles.Hr;
        ViewBag.LoginRole = selectedRole;
        return View(new LoginViewModel { Role = selectedRole });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.LoginRole = model.Role;
        if (!ModelState.IsValid) return View(model);

        if (model.Role != AccountRoles.Hr && model.Role != AccountRoles.Employee)
            ModelState.AddModelError(nameof(model.Role), "Choose a valid sign-in type.");
        if (!ModelState.IsValid) return View(model);

        var account = await _db.UserAccounts.Include(a => a.Employee)
            .SingleOrDefaultAsync(a => a.Email.ToUpper() == model.Email.Trim().ToUpper());
        if (account == null || account.Role != model.Role || _hasher.VerifyHashedPassword(account, account.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "We couldn't match that email and password. Please try again.");
            return View(model);
        }

        await SignIn(account, model.RememberMe);
        if (account.Role == AccountRoles.Hr && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
        if (account.Role == AccountRoles.Employee && account.MustChangePassword)
            return RedirectToAction("Profile", "Portal");
        return RedirectByRole(account.Role);
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.FullName))
            ModelState.AddModelError(nameof(model.FullName), "Enter your full name.");
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "Enter your work email.");
        if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            ModelState.AddModelError(nameof(model.PhoneNumber), "Enter your phone number.");
        if (!ModelState.IsValid) return View(model);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await AcquireRegistrationLockAsync(model.Email);

            var emailKey = model.Email.ToUpperInvariant();
            var emailAlreadyUsed = await _db.UserAccounts.AnyAsync(a => a.Email.ToUpper() == emailKey) ||
                await _db.Employees.AnyAsync(e => e.Email.ToUpper() == emailKey);
            var normalizedPhone = NormalizePhone(model.PhoneNumber);
            var phoneAlreadyUsed = (await _db.Employees.Select(e => e.PhoneNumber).ToListAsync())
                .Any(phone => NormalizePhone(phone) == normalizedPhone);
            if (emailAlreadyUsed || phoneAlreadyUsed)
            {
                ModelState.AddModelError(string.Empty, "An account cannot be created with those details.");
                return View(model);
            }

            var employee = new Employee
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            var account = new UserAccount
            {
                FullName = employee.FullName,
                Email = employee.Email,
                Role = AccountRoles.Employee,
                EmployeeId = employee.Id,
                MustChangePassword = false
            };
            account.PasswordHash = _hasher.HashPassword(account, model.Password);
            _db.UserAccounts.Add(account);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            await SignIn(account, false);
            return RedirectToAction("Index", "Portal");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "We couldn't create the account. Please review your details and try again.");
            return View(model);
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "We couldn't create the account right now. Please try again.");
            return View(model);
        }
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!_emailSender.IsConfigured)
        {
            ViewBag.EmailNotConfigured = true;
            ViewBag.RequestSubmitted = true;
            return View(model);
        }

        var account = await _db.UserAccounts.SingleOrDefaultAsync(a => a.Email.ToUpper() == model.Email.Trim().ToUpper());
        if (account != null)
        {
            var oldTokens = await _db.PasswordResetTokens.Where(t => t.UserAccountId == account.Id).ToListAsync();
            _db.PasswordResetTokens.RemoveRange(oldTokens);
            var rawToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var reset = new PasswordResetToken
            {
                UserAccountId = account.Id,
                TokenHash = HashResetToken(rawToken),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            };
            _db.PasswordResetTokens.Add(reset);
            await _db.SaveChangesAsync();
            var resetLink = Url.Action(nameof(ResetPassword), "Account", new { token = rawToken }, Request.Scheme);
            var safeLink = HtmlEncoder.Default.Encode(resetLink ?? string.Empty);
            try
            {
                await _emailSender.SendAsync(account.Email, "Reset your PeopleDesk password",
                    $"<p>We received a request to reset your PeopleDesk password.</p><p><a href=\"{safeLink}\">Reset password</a></p><p>This link expires in 30 minutes. If you didn't request it, you can ignore this email.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset email could not be sent.");
                _db.PasswordResetTokens.Remove(reset);
                await _db.SaveChangesAsync();
                ViewBag.EmailDeliveryFailed = true;
            }
        }
        ViewBag.RequestSubmitted = true;
        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !await IsValidResetToken(token))
        {
            ViewBag.InvalidReset = true;
            return View(new ResetPasswordViewModel());
        }
        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var tokenIsValid = !string.IsNullOrWhiteSpace(model.Token) && await IsValidResetToken(model.Token);
        if (!ModelState.IsValid || !tokenIsValid)
        {
            ViewBag.InvalidReset = !tokenIsValid;
            return View(model);
        }

        var reset = await _db.PasswordResetTokens.Include(t => t.UserAccount)
            .SingleOrDefaultAsync(t => t.TokenHash == HashResetToken(model.Token) && t.UsedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow);
        if (reset?.UserAccount == null) { ViewBag.InvalidReset = true; return View(model); }
        if (reset.UserAccount.Role == AccountRoles.Employee && reset.UserAccount.EmployeeId is int employeeId)
        {
            var employee = await _db.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                var proposedHash = _hasher.HashPassword(reset.UserAccount, model.Password);
                var existingRequest = await _db.EmployeeChangeRequests.SingleOrDefaultAsync(r => r.EmployeeId == employeeId && r.Status == EmployeeChangeRequestStatuses.Pending);
                if (existingRequest != null)
                {
                    existingRequest.ProposedPasswordHash = proposedHash;
                }
                else
                {
                    _db.EmployeeChangeRequests.Add(new EmployeeChangeRequest
                    {
                        EmployeeId = employeeId,
                        FullName = employee.FullName,
                        Email = employee.Email,
                        PhoneNumber = employee.PhoneNumber,
                        ProposedPasswordHash = proposedHash
                    });
                }
            }
            TempData["EmployeeApprovalPending"] = true;
        }
        else
        {
            reset.UserAccount.PasswordHash = _hasher.HashPassword(reset.UserAccount, model.Password);
            reset.UserAccount.MustChangePassword = false;
        }
        reset.UsedAtUtc = DateTime.UtcNow;
        var unusedTokens = await _db.PasswordResetTokens.Where(t => t.UserAccountId == reset.UserAccountId && t.Id != reset.Id).ToListAsync();
        _db.PasswordResetTokens.RemoveRange(unusedTokens);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(ResetComplete));
    }

    [AllowAnonymous]
    public IActionResult ResetComplete() => View();

    [Authorize(Roles = AccountRoles.Hr)]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, Authorize(Roles = AccountRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var account = int.TryParse(accountId, out var id) ? await _db.UserAccounts.FindAsync(id) : null;
        if (account == null) return Challenge();
        if (!ModelState.IsValid) return View(model);
        if (!account.MustChangePassword && (string.IsNullOrWhiteSpace(model.CurrentPassword) || _hasher.VerifyHashedPassword(account, account.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "That current password doesn't match your account.");
            return View(model);
        }
        account.PasswordHash = _hasher.HashPassword(account, model.NewPassword);
        account.MustChangePassword = false;
        var resetTokens = await _db.PasswordResetTokens.Where(t => t.UserAccountId == account.Id).ToListAsync();
        _db.PasswordResetTokens.RemoveRange(resetTokens);
        await _db.SaveChangesAsync();
        await SignIn(account, false);
        TempData["Success"] = "Your password has been updated.";
        return RedirectToAction("Index", User.IsInRole(AccountRoles.Hr) ? "Home" : "Portal");
    }

    [Authorize(Roles = AccountRoles.Hr)]
    public async Task<IActionResult> Users()
    {
        var accounts = await _db.UserAccounts.Include(a => a.Employee).OrderBy(a => a.Role).ThenBy(a => a.FullName).ToListAsync();
        return View(accounts);
    }

    [Authorize]
    public IActionResult Profile() => View();

    [Authorize(Roles = AccountRoles.Hr)]
    public async Task<IActionResult> CreateEmployee(int? employeeId)
    {
        await LoadUnlinkedEmployees(employeeId);
        return View(new EmployeeAccountViewModel { EmployeeId = employeeId });
    }

    [HttpPost, Authorize(Roles = AccountRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(EmployeeAccountViewModel model)
    {
        if (!model.EmployeeId.HasValue)
            ModelState.AddModelError(nameof(model.EmployeeId), "Link this login to an employee profile.");
        Employee? employee = null;
        if (model.EmployeeId.HasValue)
        {
            employee = await _db.Employees.FindAsync(model.EmployeeId.Value);
            if (employee == null || await _db.UserAccounts.AnyAsync(a => a.EmployeeId == model.EmployeeId.Value))
                ModelState.AddModelError(nameof(model.EmployeeId), "Choose an employee who doesn't already have a login.");
            else if (await _db.UserAccounts.AnyAsync(a => a.Email.ToUpper() == employee.Email.ToUpper()))
                ModelState.AddModelError(nameof(model.EmployeeId), "An account already uses this employee's email.");
        }
        if (!ModelState.IsValid)
        {
            await LoadUnlinkedEmployees(model.EmployeeId);
            return View(model);
        }

        var account = new UserAccount { FullName = employee!.FullName, Email = employee.Email, EmployeeId = employee.Id, Role = AccountRoles.Employee, MustChangePassword = true };
        var temporaryPassword = CreateTemporaryPassword();
        account.PasswordHash = _hasher.HashPassword(account, temporaryPassword);
        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync();
        TempData["NewTemporaryPassword"] = temporaryPassword;
        TempData["TemporaryPasswordEmployee"] = account.FullName;
        return RedirectToAction("Details", "Employees", new { id = employee.Id });
    }

    [Authorize(Roles = AccountRoles.Hr)]
    public async Task<IActionResult> ResetEmployeePassword(int id)
    {
        var account = await _db.UserAccounts.Include(a => a.Employee).SingleOrDefaultAsync(a => a.EmployeeId == id);
        return account == null ? NotFound() : View(account);
    }

    [HttpPost, Authorize(Roles = AccountRoles.Hr), ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetEmployeePasswordConfirmed(int id)
    {
        var account = await _db.UserAccounts.Include(a => a.Employee).SingleOrDefaultAsync(a => a.EmployeeId == id);
        if (account == null) return NotFound();
        var temporaryPassword = CreateTemporaryPassword();
        account.PasswordHash = _hasher.HashPassword(account, temporaryPassword);
        account.MustChangePassword = true;
        var pendingProfileRequests = await _db.EmployeeChangeRequests.Include(r => r.Employee).Where(r => r.EmployeeId == id && r.Status == EmployeeChangeRequestStatuses.Pending).ToListAsync();
        foreach (var request in pendingProfileRequests)
        {
            request.ProposedPasswordHash = null;
            if (request.FullName == request.Employee.FullName && request.Email == request.Employee.Email && request.PhoneNumber == request.Employee.PhoneNumber)
            {
                request.Status = EmployeeChangeRequestStatuses.Rejected;
                request.ReviewedAtUtc = DateTime.UtcNow;
                request.ReviewNote = "The password request was replaced by an HR password reset.";
            }
        }
        var pendingTokens = await _db.PasswordResetTokens.Where(t => t.UserAccountId == account.Id).ToListAsync();
        _db.PasswordResetTokens.RemoveRange(pendingTokens);
        await _db.SaveChangesAsync();
        TempData["NewTemporaryPassword"] = temporaryPassword;
        TempData["TemporaryPasswordEmployee"] = account.FullName;
        return RedirectToAction("Details", "Employees", new { id });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task LoadUnlinkedEmployees(int? selectedEmployeeId = null)
    {
        var linked = _db.UserAccounts.Where(a => a.EmployeeId != null).Select(a => a.EmployeeId);
        var employees = await _db.Employees.Where(e => !linked.Contains(e.Id) || e.Id == selectedEmployeeId).OrderBy(e => e.FullName).ToListAsync();
        ViewBag.Employees = new SelectList(employees, "Id", "FullName", selectedEmployeeId);
    }

    private async Task SignIn(UserAccount account, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.FullName),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.Role),
            new("MustChangePassword", account.MustChangePassword.ToString())
        };
        if (account.EmployeeId.HasValue) claims.Add(new Claim("EmployeeId", account.EmployeeId.Value.ToString()));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = rememberMe, AllowRefresh = true });
    }

    private async Task<bool> IsValidResetToken(string token) =>
        await _db.PasswordResetTokens.AnyAsync(t => t.TokenHash == HashResetToken(token) && t.UsedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow);

    private static string HashResetToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string CreateTemporaryPassword()
    {
        var value = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
        return value + "a7!";
    }

    private async Task AcquireRegistrationLockAsync(string email)
    {
        await _db.Database.OpenConnectionAsync();
        await using var command = _db.Database.GetDbConnection().CreateCommand();
        command.Transaction = _db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sp_getapplock @Resource = @resource, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 5000; SELECT @result;";

        var resourceParameter = command.CreateParameter();
        resourceParameter.ParameterName = "@resource";
        resourceParameter.Value = "EmployeeManagementSystem.EmployeeRegistration:" + email.ToUpperInvariant();
        command.Parameters.Add(resourceParameter);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (result < 0)
            throw new InvalidOperationException("The employee registration lock could not be acquired.");
    }

    private static string NormalizePhone(string phoneNumber) =>
        new(phoneNumber.Where(char.IsDigit).ToArray());

    private IActionResult RedirectByRole(string? role = null) =>
        RedirectToAction("Index", (role ?? User.FindFirstValue(ClaimTypes.Role)) == AccountRoles.Hr ? "Home" : "Portal");
}
