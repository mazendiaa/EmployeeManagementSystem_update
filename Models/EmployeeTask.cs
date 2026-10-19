using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManagementSystem.Models;

public static class EmployeeTaskStatuses
{
    public const string Assigned = "Assigned";
    public const string InProgress = "In progress";
    public const string Completed = "Completed";
}

public class EmployeeTask
{
    public int Id { get; set; }
    [Required] public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    [Required, StringLength(140)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [DataType(DataType.Date), Column(TypeName = "date"), Display(Name = "Due date")] public DateTime? DueDate { get; set; }
    [Required, StringLength(24)] public string Status { get; set; } = EmployeeTaskStatuses.Assigned;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
