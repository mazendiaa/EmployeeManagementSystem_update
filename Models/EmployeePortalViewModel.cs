namespace EmployeeManagementSystem.Models;

public class EmployeePortalViewModel
{
    public Employee Employee { get; set; } = new();
    public int Month { get; set; }
    public int Year { get; set; }
    public List<AttendanceRecord> Attendance { get; set; } = [];
    public List<EmployeeTask> Tasks { get; set; } = [];
    public double TotalHours => Attendance.Where(a => a.ClockIn.HasValue && a.ClockOut.HasValue).Sum(a => (a.ClockOut!.Value - a.ClockIn!.Value).TotalHours);
}

public class WorkTrackingViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public List<Employee> Employees { get; set; } = [];
    public List<AttendanceRecord> Attendance { get; set; } = [];
    public List<EmployeeTask> Tasks { get; set; } = [];
}
