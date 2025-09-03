using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using BCrypt.Net;

public static class DbSeeder
{
    public static async Task SeedDataAsync(ApplicationDbContext context)
    {
        // This is a safety check to avoid re-seeding if data already exists.
        // It checks if an admin user is present.
        if (await context.SystemUsers.AnyAsync(u => u.UserType == 1))
        {
            return;
        }

        // Drop and recreate the database to ensure a clean state
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        // Create a default department
        var department = new Department
        {
            DepartmentName="IT",
        };
        await context.Departments.AddAsync(department);
        await context.SaveChangesAsync();

        // Create the admin employee
        var adminEmployee = new Employee
        {
            EmployeeName = "Admin User",
            EmployeeId = "A-001", // Corrected type: string
            EmployeePhotoPath = "/images/default_admin.jpg",
            DateOfBirth = new DateTime(1985, 1, 1),
            Gender = "Male",
            DepartmentId = department.DepartmentId,
            Salary = 100000.00m,
            Nationality = "Bangladeshi",
            Description = "System Administrator"
        };
        await context.Employees.AddAsync(adminEmployee);
        await context.SaveChangesAsync(); // First save to get the auto-generated 'Id'

        // Create the admin SystemUser
        var adminUser = new SystemUser
        {
            Email = "admin2025@gmail.com",
            PasswordHash = PasswordHasher.HashPassword("Admin@123"),
            UserType = 1, // Admin
            EmployeeId = adminEmployee.Id // Corrected: link to the auto-generated 'Id'
        };
        await context.SystemUsers.AddAsync(adminUser);

        // Create a regular employee
        var regularEmployee = new Employee
        {
            EmployeeName = "John Doe",
            EmployeeId = "E-001", // Corrected type: string
            EmployeePhotoPath = "/images/default_user.jpg",
            DateOfBirth = new DateTime(1990, 5, 15),
            Gender = "Male",
            DepartmentId = department.DepartmentId,
            Salary = 50000.00m,
            Nationality = "Bangladeshi",
            Description = "Software Developer"
        };
        await context.Employees.AddAsync(regularEmployee);
        await context.SaveChangesAsync(); // First save to get the auto-generated 'Id'

        // Create a regular SystemUser
        var regularUser = new SystemUser
        {
            Email = "john.doe@smartattendancesystem.com",
            PasswordHash = PasswordHasher.HashPassword("User@123"),
            UserType = 2, // General User
            EmployeeId = regularEmployee.Id // Corrected: link to the auto-generated 'Id'
        };
        await context.SystemUsers.AddAsync(regularUser);

        await context.SaveChangesAsync();
    }
}