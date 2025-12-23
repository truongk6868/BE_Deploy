using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondotelManagement.Models
{
    [Table("BlogRequests")]
    public partial class BlogRequest
    {
        [Key]
        public int BlogRequestId { get; set; }

        [Required]
        public int HostId { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "datetime2(0)")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(0)")]
        public DateTime? ProcessedDate { get; set; }

        public int? ProcessedByUserId { get; set; }

        public string? RejectionReason { get; set; }

        // Navigation properties
        [ForeignKey("HostId")]
        public virtual Host Host { get; set; } = null!;

        [ForeignKey("ProcessedByUserId")]
        public virtual User? ProcessedByUser { get; set; }
        [StringLength(500)] // Thêm độ dài tùy ý
        public string? FeaturedImageUrl { get; set; }
        [Column("CategoryID")]
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual BlogCategory BlogCategory { get; set; }
    }
}
