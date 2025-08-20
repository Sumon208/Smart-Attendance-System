using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Attendance_System.Models
{
    public enum EmployeeStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string EmployeeName { get; set; }

        [Required]
        [StringLength(500)]
        public string EmployeePhotoPath { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? JoiningDate { get; set; }

        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Salary { get; set; }

        public string? Nationality { get; set; }

        public string? Description { get; set; }

        // New properties
        public string? MobileNumber { get; set; }
        public string? BloodGroup { get; set; }
        public string? CertificateFilePath { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime? JoiningDate { get; set; }

        // Employee status for approval process
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Pending;

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
    }
}