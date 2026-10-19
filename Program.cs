using EmployeeManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "PeopleDesk.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(10);
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var accountId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(accountId, out var id))
            {
                context.RejectPrincipal();
                return;
            }

            var account = await db.UserAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
            var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
            var name = context.Principal?.FindFirstValue(ClaimTypes.Name);
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var employeeId = context.Principal?.FindFirstValue("EmployeeId");
            var employeeClaimIsValid = account?.Role == AccountRoles.Employee &&
                account.EmployeeId.HasValue &&
                int.TryParse(employeeId, out var claimedEmployeeId) &&
                claimedEmployeeId == account.EmployeeId.Value;
            var roleIsValid = (account?.Role == AccountRoles.Hr || account?.Role == AccountRoles.Employee) &&
                (account.Role == AccountRoles.Hr ? !account.EmployeeId.HasValue && string.IsNullOrEmpty(employeeId) : employeeClaimIsValid);
            var accountClaimsAreCurrent = account != null && name == account.FullName && email == account.Email;

            if (account == null || role != account.Role || !roleIsValid || !accountClaimsAreCurrent)
            {
                context.RejectPrincipal();
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<InitialHrProvisioner>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Apply pending migrations and add the sample catalog once for a fresh database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(db);
    await scope.ServiceProvider.GetRequiredService<InitialHrProvisioner>().ProvisionAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
