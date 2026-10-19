using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class PasswordResetToken
{
    public int Id { get; set; }
    [Required] public int UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }
    [Required, StringLength(64)] public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
