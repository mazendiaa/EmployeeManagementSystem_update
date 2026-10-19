using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class EmployeeChangeRequestInputModel
{
    [Required, StringLength(120), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150), Display(Name = "Work email")]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(30), Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
    [StringLength(100), DataType(DataType.Password), Display(Name = "New password")]
    public string? NewPassword { get; set; }
    [Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Confirm new password")]
    public string? ConfirmPassword { get; set; }
}
