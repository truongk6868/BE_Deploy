using System.ComponentModel.DataAnnotations;
namespace CondotelManagement.DTOs.Tenant
{
    public class ReviewDTO
    {
        [Required(ErrorMessage = "BookingID is required")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "CondotelID is required")]
        public int CondotelId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public byte Rating { get; set; }

        [MaxLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string? Comment { get; set; }
    }
}
