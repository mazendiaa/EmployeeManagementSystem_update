using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class ChangePasswordViewModel
{
    [DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(10), DataType(DataType.Password), Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;
    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
