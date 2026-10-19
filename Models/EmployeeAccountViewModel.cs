using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementSystem.Models;

public class EmployeeAccountViewModel
{
    [Required, Display(Name = "Employee profile")]
    public int? EmployeeId { get; set; }
}
