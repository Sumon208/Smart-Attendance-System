using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class AttendanceHistoryViewModel
    {
        public IEnumerable<Attendance> Attendances { get; set; } = new List<Attendance>();
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        
        // Filter properties
        public string Status { get; set; } = "";
        public string DateFrom { get; set; } = "";
        public string DateTo { get; set; } = "";
        public List<string> StatusOptions { get; set; } = new List<string>();
        
        // Statistics
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public double AttendanceRate { get; set; }
        public double AverageWorkingHours { get; set; }
        
        // Helper properties for pagination
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int PreviousPage => CurrentPage - 1;
        public int NextPage => CurrentPage + 1;
        
        // Range for page numbers display
        public int StartPage => Math.Max(1, CurrentPage - 2);
        public int EndPage => Math.Min(TotalPages, CurrentPage + 2);
    }
}
