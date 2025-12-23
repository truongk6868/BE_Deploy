using System;

namespace CondotelManagement.DTOs.Package
{
    public class HostPackageDetailsDto
    {
        public string PackageName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? StartDate { get; set; }   // FE nhận string, có thể null
        public string? EndDate { get; set; }     // FE nhận string, có thể null
        public int MaxListings { get; set; }
        public int CurrentListings { get; set; }
        public bool CanUseFeaturedListing { get; set; }

        // === THÊM CÁC TRƯỜNG MỚI CHO FEATURES ===
        public int MaxBlogRequestsPerMonth { get; set; }
        public int UsedBlogRequestsThisMonth { get; set; }
        public bool IsVerifiedBadgeEnabled { get; set; }
        public string? DisplayColorTheme { get; set; }
        public int PriorityLevel { get; set; }

        // === CÁC TRƯỜNG CHO PAYOS ===
        public string? Message { get; set; }
        public string? PaymentUrl { get; set; }
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
    }
}