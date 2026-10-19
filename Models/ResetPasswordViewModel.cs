using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class ResetPasswordViewModel
{
    [Required] public string Token { get; set; } = string.Empty;
    [Required, MinLength(10), DataType(DataType.Password), Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
