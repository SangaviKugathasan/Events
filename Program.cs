using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using EventZax.Data;
using Serilog;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.IO;
using EventZax.Models;
using System.Data.Common;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DinkToPdf converter for PDF generation
builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));

// Configure Entity Framework Core with SQLite for development
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Configure ASP.NET Identity with ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false; // For development
    options.Password.RequireDigit = false; // For development
    options.Password.RequireLowercase = false; // For development
    options.Password.RequireNonAlphanumeric = false; // For development
    options.Password.RequireUppercase = false; // For development
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // This will create the database if it doesn't exist

        // Runtime schema fixes for older DBs
        try
        {
            var conn = context.Database.GetDbConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                // Only proceed if Events table exists
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Events';";
                var tbl = cmd.ExecuteScalar();
                if (tbl != null)
                {
                    // Check column list
                    cmd.CommandText = "PRAGMA table_info('Events');";
                    bool hasIsPublished = false;
                    bool hasOrganizerId = false;
                    bool hasImagePath = false;
                    bool hasVenueName = false;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var colName = reader.GetString(1);
                            if (string.Equals(colName, "IsPublished", System.StringComparison.OrdinalIgnoreCase))
                                hasIsPublished = true;
                            if (string.Equals(colName, "OrganizerId", System.StringComparison.OrdinalIgnoreCase))
                                hasOrganizerId = true;
                            if (string.Equals(colName, "ImagePath", System.StringComparison.OrdinalIgnoreCase))
                                hasImagePath = true;
                            if (string.Equals(colName, "VenueName", System.StringComparison.OrdinalIgnoreCase))
                                hasVenueName = true;
                        }
                    }

                    if (!hasIsPublished)
                    {
                        using (var addCmd = conn.CreateCommand())
                        {
                            addCmd.CommandText = "ALTER TABLE Events ADD COLUMN IsPublished INTEGER NOT NULL DEFAULT 0;";
                            addCmd.ExecuteNonQuery();
                        }
                    }

                    if (!hasOrganizerId)
                    {
                        using (var addCmd = conn.CreateCommand())
                        {
                            addCmd.CommandText = "ALTER TABLE Events ADD COLUMN OrganizerId TEXT;";
                            addCmd.ExecuteNonQuery();
                        }
                    }

                    if (!hasImagePath)
                    {
                        using (var addCmd = conn.CreateCommand())
                        {
                            addCmd.CommandText = "ALTER TABLE Events ADD COLUMN ImagePath TEXT DEFAULT ''";
                            addCmd.ExecuteNonQuery();
                        }
                    }

                    if (!hasVenueName)
                    {
                        using (var addCmd = conn.CreateCommand())
                        {
                            addCmd.CommandText = "ALTER TABLE Events ADD COLUMN VenueName TEXT DEFAULT ''";
                            addCmd.ExecuteNonQuery();
                        }
                    }
                }

                // Ensure Attendances table exists
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Attendances';";
                var attTbl = cmd.ExecuteScalar();
                if (attTbl == null)
                {
                    using (var create = conn.CreateCommand())
                    {
                        create.CommandText = @"CREATE TABLE IF NOT EXISTS Attendances (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            EventId INTEGER NOT NULL,
                            UserId TEXT,
                            CheckInTime TEXT,
                            IsCheckedIn INTEGER NOT NULL DEFAULT 0,
                            FullName TEXT,
                            Address TEXT,
                            Tel TEXT
                        );";
                        create.ExecuteNonQuery();
                    }
                }
            }
            conn.Close();
        }
        catch
        {
            // Ignore schema-fix failures; migrations will report errors.
        }

        // --- ADMIN SEEDING LOGIC START ---
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure roles exist
        string[] roles = new[] { "Admin", "Organizer", "Customer" };
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).Result)
                roleManager.CreateAsync(new IdentityRole(role)).Wait();
        }

        // Create admin user if it doesn't exist
        var adminEmail = "admin@gmail.com";
        var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, IsApproved = true };
            userManager.CreateAsync(adminUser, "admin123").Wait();
            userManager.AddToRoleAsync(adminUser, "Admin").Wait();
        }
        else
        {
            // Ensure admin user has correct username and is in Admin role
            if (adminUser.UserName != adminEmail)
            {
                adminUser.UserName = adminEmail;
                userManager.UpdateAsync(adminUser).Wait();
            }
            if (!userManager.IsInRoleAsync(adminUser, "Admin").Result)
            {
                userManager.AddToRoleAsync(adminUser, "Admin").Wait();
            }
            if (!adminUser.IsApproved)
            {
                adminUser.IsApproved = true;
                userManager.UpdateAsync(adminUser).Wait();
            }
            // Reset password to admin123
            var token = userManager.GeneratePasswordResetTokenAsync(adminUser).Result;
            userManager.ResetPasswordAsync(adminUser, token, "admin123").Wait();
        }
        // --- ADMIN SEEDING LOGIC END ---
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add a small middleware to respond to Chrome DevTools appspecific probe to avoid 404 noise
app.Use(async (context, next) =>
{
    var probePath = "/.well-known/appspecific/com.chrome.devtools.json";
    if (context.Request.Path.Equals(probePath, StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, ".well-known", "appspecific", "com.chrome.devtools.json");
            if (System.IO.File.Exists(filePath))
            {
                context.Response.ContentType = "application/json";
                await context.Response.SendFileAsync(filePath);
                return;
            }
        }
        catch
        {
            // ignore file serve errors and fall back to inline JSON
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"name\":\"com.chrome.devtools\",\"version\":\"1.0\"}");
        return;
    }
    await next();
});

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
