using System.ComponentModel.DataAnnotations;
using System.Data;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EmployeeManagementSystem.Services;

public sealed class InitialHrProvisioner
{
    private const string ProvisioningLockResource = "EmployeeManagementSystem.InitialHrProvisioning";
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<UserAccount> _hasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InitialHrProvisioner> _logger;

    public InitialHrProvisioner(
        AppDbContext db,
        IPasswordHasher<UserAccount> hasher,
        IConfiguration configuration,
        ILogger<InitialHrProvisioner> logger)
    {
        _db = db;
        _hasher = hasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProvisionAsync(CancellationToken cancellationToken = default)
    {
        var settings = ReadSettings();
        if (!settings.Enabled)
        {
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await AcquireProvisioningLockAsync(cancellationToken);

        if (await _db.UserAccounts.AnyAsync(a => a.Role == AccountRoles.Hr, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var account = new UserAccount
        {
            FullName = settings.FullName,
            Email = settings.Email,
            Role = AccountRoles.Hr,
            MustChangePassword = false
        };
        account.PasswordHash = _hasher.HashPassword(account, settings.Password);

        _db.UserAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("The initial HR account was provisioned from deployment configuration.");
    }

    private async Task AcquireProvisioningLockAsync(CancellationToken cancellationToken)
    {
        await _db.Database.OpenConnectionAsync(cancellationToken);
        await using var command = _db.Database.GetDbConnection().CreateCommand();
        command.Transaction = _db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sp_getapplock @Resource = @resource, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 0; SELECT @result;";

        var resourceParameter = command.CreateParameter();
        resourceParameter.ParameterName = "@resource";
        resourceParameter.Value = ProvisioningLockResource;
        command.Parameters.Add(resourceParameter);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException("The initial HR provisioning lock could not be acquired.");
        }
    }

    private ProvisioningSettings ReadSettings()
    {
        var section = _configuration.GetSection("InitialHrProvisioning");
        var enabled = section.GetValue<bool>("Enabled");
        if (!enabled)
        {
            return ProvisioningSettings.Disabled;
        }

        var fullName = section["FullName"]?.Trim();
        var email = section["Email"]?.Trim();
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 120)
        {
            throw new InvalidOperationException("Initial HR provisioning requires a valid FullName configuration value.");
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 150 || !new EmailAddressAttribute().IsValid(email))
        {
            throw new InvalidOperationException("Initial HR provisioning requires a valid Email configuration value.");
        }

        if (string.IsNullOrEmpty(password) || password.Length < 10)
        {
            throw new InvalidOperationException("Initial HR provisioning requires a Password configuration value of at least 10 characters.");
        }

        return new ProvisioningSettings(true, fullName, email, password);
    }

    private sealed record ProvisioningSettings(bool Enabled, string FullName, string Email, string Password)
    {
        public static ProvisioningSettings Disabled { get; } = new(false, string.Empty, string.Empty, string.Empty);
    }
}
