using CRMPortal.Data;
using CRMPortal.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ===========================
// Add Services
// ===========================

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\Keys"))
    .SetApplicationName("CRMPortal");

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(15);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.MaxAge = TimeSpan.FromHours(15);

    options.Cookie.Name = ".CRMPortal.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});


// ===========================
// ADD THESE TWO LINES
// ===========================
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ErrorLogService>();

// Existing Service
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// ===========================
// Configure HTTP Request Pipeline
// ===========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Uncomment only after HTTPS certificate is configured on IIS
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Session must come before Authorization
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();