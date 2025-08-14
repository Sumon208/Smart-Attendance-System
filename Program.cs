using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Services.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Smart_Attendance_System
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // === Add Database and Authentication Services Here ===

            // Read the connection string from appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("default");

            // Configure DbContext to use SQL Server
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Configure cookie-based authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                    options.SlidingExpiration = true;
                });

            // Register Repositories for Dependency Injection
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();

            var app = builder.Build();

            // === Call the DbSeeder to populate the database ===

            // Create a scope to get services, including DbContext
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    // Call the asynchronous seeding method
                    await DbSeeder.SeedDataAsync(context);
                }
                catch (Exception ex)
                {
                    // Log any errors that occur during seeding
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Add authentication middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Map the default controller and action
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}