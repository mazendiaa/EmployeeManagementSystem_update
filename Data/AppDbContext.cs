using EmployeeManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<JobTitle> JobTitles { get; set; }
    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<EmployeeTask> EmployeeTasks { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<EmployeeChangeRequest> EmployeeChangeRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.EmployeeId).IsUnique();
        modelBuilder.Entity<UserAccount>()
            .HasOne(u => u.Employee).WithMany().HasForeignKey(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttendanceRecord>().HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique();
        modelBuilder.Entity<AttendanceRecord>().HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeTask>().HasOne(t => t.Employee).WithMany().HasForeignKey(t => t.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PasswordResetToken>().HasIndex(t => t.TokenHash).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasOne(t => t.UserAccount).WithMany(a => a.PasswordResetTokens).HasForeignKey(t => t.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeChangeRequest>().HasIndex(r => r.EmployeeId).HasFilter("[Status] = N'Pending'").IsUnique();
        modelBuilder.Entity<EmployeeChangeRequest>().HasIndex(r => new { r.EmployeeId, r.RequestedAtUtc });
        modelBuilder.Entity<EmployeeChangeRequest>().HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmployeeChangeRequest>().HasOne(r => r.ReviewedBy).WithMany().HasForeignKey(r => r.ReviewedByUserAccountId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.JobTitle)
            .WithMany(j => j.Employees)
            .HasForeignKey(e => e.JobTitleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
