using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Payment
{
    public class PaymentRequestDTO
    {
        [Required(ErrorMessage = "BookingId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "BookingId must be greater than 0")]
        public int BookingId { get; set; }
        
        public string? Description { get; set; }
        
        public string? ReturnUrl { get; set; }
        
        public string? CancelUrl { get; set; }
    }
}


