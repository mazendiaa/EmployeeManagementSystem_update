using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeManagementSystem.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    [Required] public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    [Required, DataType(DataType.Date), Column(TypeName = "date"), Display(Name = "Work date")] public DateTime WorkDate { get; set; } = DateTime.Today;
    [Display(Name = "Clock in")] public TimeSpan? ClockIn { get; set; }
    [Display(Name = "Clock out")] public TimeSpan? ClockOut { get; set; }
    [StringLength(400)] public string? Notes { get; set; }
}
