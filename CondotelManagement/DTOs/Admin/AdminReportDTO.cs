using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Admin
{
    public class AdminReportCreateDTO
    {
        [Required(ErrorMessage = "ReportType là bắt buộc")]
        public string ReportType { get; set; } = string.Empty; // "HostReport", "SystemReport", "AllHostsReport", etc.
        
        public int? HostId { get; set; } // Optional: Nếu là report cho host cụ thể, null = tất cả hosts
        
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
    }

    public class AdminReportResponseDTO
    {
        public int ReportId { get; set; }
        public int AdminId { get; set; }
        public string? AdminName { get; set; }
        public string? ReportType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
    }

    public class AdminReportListDTO
    {
        public int ReportId { get; set; }
        public int AdminId { get; set; }
        public string? AdminName { get; set; }
        public string? ReportType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public int? HostId { get; set; }
        public string? HostName { get; set; }
    }
}

