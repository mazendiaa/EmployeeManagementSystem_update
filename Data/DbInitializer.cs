using EmployeeManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Name = "Engineering" },
                new Department { Name = "Human Resources" },
                new Department { Name = "Finance" },
                new Department { Name = "Marketing" },
                new Department { Name = "Sales" },
                new Department { Name = "Operations" });
            await db.SaveChangesAsync();
        }

        if (!await db.JobTitles.AnyAsync())
        {
            db.JobTitles.AddRange(
                new JobTitle { Title = "Software Engineer" },
                new JobTitle { Title = "Senior Software Engineer" },
                new JobTitle { Title = "QA Engineer" },
                new JobTitle { Title = "HR Specialist" },
                new JobTitle { Title = "Accountant" },
                new JobTitle { Title = "Marketing Specialist" },
                new JobTitle { Title = "Sales Representative" },
                new JobTitle { Title = "Project Manager" },
                new JobTitle { Title = "Business Analyst" },
                new JobTitle { Title = "Operations Coordinator" });
            await db.SaveChangesAsync();
        }

        if (await db.Employees.AnyAsync())
        {
            return;
        }

        var departments = await db.Departments.ToDictionaryAsync(d => d.Name, d => d.Id);
        var jobTitles = await db.JobTitles.ToDictionaryAsync(j => j.Title, j => j.Id);

        var sampleEmployees = new (string Name, string Email, string Phone, DateTime HireDate, decimal Salary, string Department, string Title)[]
        {
            ("Omar Hassan", "omar.hassan@example.com", "+20 100 234 5678", new DateTime(2022, 3, 14), 32000m, "Engineering", "Senior Software Engineer"),
            ("Mariam Adel", "mariam.adel@example.com", "+20 101 345 6789", new DateTime(2023, 7, 2), 24500m, "Engineering", "Software Engineer"),
            ("Youssef Nabil", "youssef.nabil@example.com", "+20 102 456 7890", new DateTime(2021, 11, 21), 27000m, "Engineering", "QA Engineer"),
            ("Nour El-Sayed", "nour.elsayed@example.com", "+20 103 567 8901", new DateTime(2024, 1, 8), 18500m, "Human Resources", "HR Specialist"),
            ("Salma Mahmoud", "salma.mahmoud@example.com", "+20 104 678 9012", new DateTime(2020, 5, 17), 30000m, "Finance", "Accountant"),
            ("Karim Fathy", "karim.fathy@example.com", "+20 105 789 0123", new DateTime(2022, 9, 5), 22500m, "Marketing", "Marketing Specialist"),
            ("Farah Mostafa", "farah.mostafa@example.com", "+20 106 890 1234", new DateTime(2023, 2, 12), 21000m, "Sales", "Sales Representative"),
            ("Ahmed Samir", "ahmed.samir@example.com", "+20 109 123 4567", new DateTime(2019, 8, 25), 42000m, "Operations", "Project Manager"),
            ("Dina Tarek", "dina.tarek@example.com", "+20 110 234 5678", new DateTime(2021, 4, 19), 28000m, "Engineering", "Business Analyst"),
            ("Hassan Ehab", "hassan.ehab@example.com", "+20 111 345 6789", new DateTime(2024, 6, 3), 19500m, "Operations", "Operations Coordinator"),
            ("Laila Ashraf", "laila.ashraf@example.com", "+20 112 456 7890", new DateTime(2022, 12, 11), 23500m, "Sales", "Sales Representative"),
            ("Mostafa Wael", "mostafa.wael@example.com", "+20 115 567 8901", new DateTime(2020, 10, 1), 34000m, "Engineering", "Senior Software Engineer")
        };

        foreach (var sample in sampleEmployees)
        {
            // Skip a sample row if a partially populated catalog does not contain its references.
            if (!departments.TryGetValue(sample.Department, out var departmentId) ||
                !jobTitles.TryGetValue(sample.Title, out var jobTitleId))
            {
                continue;
            }

            db.Employees.Add(new Employee
            {
                FullName = sample.Name,
                Email = sample.Email,
                PhoneNumber = sample.Phone,
                HireDate = sample.HireDate,
                Salary = sample.Salary,
                DepartmentId = departmentId,
                JobTitleId = jobTitleId
            });
        }

        await db.SaveChangesAsync();
    }
}
