namespace CondotelManagement.DTOs.Blog
{
    // Dùng khi xem chi tiết 1 bài (có nội dung)
    public class BlogPostDetailDto : BlogPostSummaryDto
    {
        public string Content { get; set; } = null!;
        public int? CategoryId { get; set; }
        public string Status { get; set; } = null!;

    }
}