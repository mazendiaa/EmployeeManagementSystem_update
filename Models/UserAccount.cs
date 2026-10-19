using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public static class AccountRoles
{
    public const string Hr = "HR";
    public const string Employee = "Employee";
}

public class UserAccount
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(500)] public string PasswordHash { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Role { get; set; } = AccountRoles.Employee;
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool MustChangePassword { get; set; }
    public List<PasswordResetToken> PasswordResetTokens { get; set; } = [];
}
