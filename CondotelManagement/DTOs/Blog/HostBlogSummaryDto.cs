namespace CondotelManagement.DTOs.Blog
{
    // DTOs/Blog/HostBlogSummaryDto.cs
    public class HostBlogSummaryDto
    {
        public int Id { get; set; } // BlogRequestId
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Thumbnail { get; set; }
        public string Status { get; set; } // "PENDING", "APPROVED", "REJECTED"
        public string RejectionReason { get; set; } // 👈 Quan trọng: Lý do từ chối
        public DateTime CreatedAt { get; set; }
        public string Content { get; set; }      // Để load lại nội dung vào khung soạn thảo
        public int? CategoryId { get; set; }
    }
}
