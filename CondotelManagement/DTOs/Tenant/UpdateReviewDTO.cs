using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Tenant
{
    public class UpdateReviewDTO
    {
        [Required(ErrorMessage = "ReviewID is required")]
        public int ReviewId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public byte Rating { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string? Comment { get; set; }
    }
}
