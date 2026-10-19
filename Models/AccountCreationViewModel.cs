using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class AccountCreationViewModel
{
    [StringLength(120), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;
    [EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
    [Required, MinLength(10), DataType(DataType.Password), Display(Name = "Temporary password")]
    public string Password { get; set; } = string.Empty;
    [Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
}
