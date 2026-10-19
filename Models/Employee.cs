using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManagementSystem.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date joined")]
    public DateTime? HireDate { get; set; }

    [Range(1, 1000000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Salary { get; set; }

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Job Title")]
    public int? JobTitleId { get; set; }

    [StringLength(250)]
    public string? ProfileImagePath { get; set; }

    public Department? Department { get; set; }
    public JobTitle? JobTitle { get; set; }
}
