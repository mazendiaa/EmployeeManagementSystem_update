namespace EmployeeManagementSystem.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, string subject, string htmlBody);
}
