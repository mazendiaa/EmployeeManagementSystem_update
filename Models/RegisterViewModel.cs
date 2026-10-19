using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class RegisterViewModel
{
    [Required, StringLength(120), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150), Display(Name = "Work email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30), Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MinLength(10), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
