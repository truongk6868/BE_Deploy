namespace CondotelManagement.DTOs.Blog
{
    public class HostBlogRequestDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public int? CategoryId { get; set; }
    }

    // DTO trả về kết quả (để hiển thị còn bao nhiêu lượt)
    public class BlogRequestResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int RemainingQuota { get; set; } // Số lượt còn lại
        public string CurrentPackage { get; set; } = null!;
    }
}