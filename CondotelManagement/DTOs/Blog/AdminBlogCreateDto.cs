using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Blog
{
    public class AdminBlogCreateDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        public string? FeaturedImageUrl { get; set; }

        [Required]
        [Range(1, 10)] // Giả sử chỉ có 2 trạng thái
        public string Status { get; set; } = "Draft"; // "Draft" hoặc "Published"

        public int? CategoryId { get; set; }
    }
}