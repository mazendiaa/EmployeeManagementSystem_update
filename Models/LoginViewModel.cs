using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class LoginViewModel
{
    [Required]
    public string Role { get; set; } = AccountRoles.Hr;

    [Required, EmailAddress, Display(Name = "Work email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
