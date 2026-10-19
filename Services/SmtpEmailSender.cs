using System.Net;
using System.Net.Mail;

namespace EmployeeManagementSystem.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private string Host => configuration["Email:Host"] ?? string.Empty;
    private string From => configuration["Email:From"] ?? string.Empty;
    private int Port => int.TryParse(configuration["Email:Port"], out var port) ? port : 587;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);

    public async Task SendAsync(string recipient, string subject, string htmlBody)
    {
        if (!IsConfigured) throw new InvalidOperationException("Email delivery is not configured.");
        using var message = new MailMessage(From, recipient, subject, htmlBody) { IsBodyHtml = true };
        using var client = new SmtpClient(Host, Port)
        {
            EnableSsl = bool.TryParse(configuration["Email:EnableSsl"], out var ssl) ? ssl : true,
            Timeout = 15000
        };
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password);
        await client.SendMailAsync(message);
    }
}
