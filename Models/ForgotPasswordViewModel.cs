using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress, Display(Name = "Work email")]
    public string Email { get; set; } = string.Empty;
}
