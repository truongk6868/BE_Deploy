namespace CondotelManagement.DTOs.Blog
{
    // Dùng để hiển thị danh sách bài viết (không có nội dung)
    public class BlogPostSummaryDto
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string AuthorName { get; set; } = null!; // Lấy từ User.FullName
        public string CategoryName { get; set; } = null!; // Lấy từ BlogCategory.Name
    }
}