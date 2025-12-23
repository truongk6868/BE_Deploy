namespace CondotelManagement.DTOs.Package
{
    public class PackageDto
    {
        public int PackageId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string Duration { get; set; } = null!;
        public string Description { get; set; } = null!;

        public int MaxListings { get; set; }
        public bool CanUseFeaturedListing { get; set; }

        // === THÊM CÁC TRƯỜNG MỚI ===
        public int MaxBlogRequestsPerMonth { get; set; }
        public bool IsVerifiedBadgeEnabled { get; set; }
        public string? DisplayColorTheme { get; set; }
        public int PriorityLevel { get; set; }
    }
}