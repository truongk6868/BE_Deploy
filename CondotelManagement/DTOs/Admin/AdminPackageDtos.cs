// File: DTOs/Admin/AdminPackageDtos.cs
namespace CondotelManagement.DTOs.Admin
{
    public class AdminPackageDtos
    {
        // DTO cho tạo gói mới - THÊM CÁC TRƯỜNG FEATURES
        public class PackageCreateDto
        {
            public string Name { get; set; } = null!;
            public decimal? Price { get; set; }
            public int? DurationDays { get; set; } // Ví dụ: 30 ngày, 365 ngày
            public string? Description { get; set; }

            // ======== THÊM CÁC TRƯỜNG FEATURES ========
            public int MaxListingCount { get; set; } = 0;          // Số condotel tối đa
            public bool CanUseFeaturedListing { get; set; } = false; // Được đăng tin nổi bật không
            public int MaxBlogRequestsPerMonth { get; set; } = 0;   // Số blog tối đa mỗi tháng
            public bool IsVerifiedBadgeEnabled { get; set; } = false; // Có badge xác minh không
            public string DisplayColorTheme { get; set; } = "default"; // Theme màu hiển thị
            public int PriorityLevel { get; set; } = 0;              // Mức độ ưu tiên
        }

        // DTO cho cập nhật gói - THÊM CÁC TRƯỜNG FEATURES
        public class PackageUpdateDto : PackageCreateDto
        {
            public bool IsActive { get; set; } = true;
        }

        // DTO cho response (có thể tách riêng)
        public class CatalogPackageDto
        {
            public int PackageId { get; set; }
            public string Name { get; set; } = "Chưa đặt tên";
            public decimal Price { get; set; }
            public int? DurationDays { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; }

            // ======== THÊM CÁC TRƯỜNG FEATURES ========
            public int MaxListingCount { get; set; }
            public bool CanUseFeaturedListing { get; set; }
            public int MaxBlogRequestsPerMonth { get; set; }
            public bool IsVerifiedBadgeEnabled { get; set; }
            public string DisplayColorTheme { get; set; } = "default";
            public int PriorityLevel { get; set; }
        }
    }
}