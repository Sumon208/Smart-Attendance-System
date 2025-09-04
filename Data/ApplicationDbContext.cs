using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<SystemUser> SystemUsers { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define a unique index on EmployeeId to prevent duplicates
            modelBuilder.Entity<Employee>()
               .HasIndex(e => e.EmployeeId)
               .IsUnique();

            // Define a unique index on Email for the SystemUser
            modelBuilder.Entity<SystemUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Define a one-to-one relationship between Employee and SystemUser
            modelBuilder.Entity<SystemUser>()
            .HasOne(su => su.Employee)
            .WithOne()
            .HasForeignKey<SystemUser>(su => su.EmployeeId);
        }
    }
}
