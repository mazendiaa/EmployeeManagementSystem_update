using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public static class EmployeeChangeRequestStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public class EmployeeChangeRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    [Required, StringLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(30)] public string PhoneNumber { get; set; } = string.Empty;
    [StringLength(500)] public string? ProposedPasswordHash { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = EmployeeChangeRequestStatuses.Pending;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public int? ReviewedByUserAccountId { get; set; }
    [StringLength(400)] public string? ReviewNote { get; set; }
    public Employee Employee { get; set; } = null!;
    public UserAccount? ReviewedBy { get; set; }
}
