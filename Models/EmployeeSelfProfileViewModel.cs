namespace EmployeeManagementSystem.Models;

public class EmployeeSelfProfileViewModel
{
    public Employee Employee { get; set; } = null!;
    public EmployeeChangeRequestInputModel Request { get; set; } = new();
    public EmployeeChangeRequest? PendingRequest { get; set; }
    public List<EmployeeChangeRequest> RecentRequests { get; set; } = [];
    public bool MustChangePassword { get; set; }
}
