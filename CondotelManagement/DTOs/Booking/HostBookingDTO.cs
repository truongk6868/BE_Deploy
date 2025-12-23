namespace CondotelManagement.DTOs
{
    public class HostBookingDTO
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }

        public string CondotelName { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public DateTime BookingDate { get; set; }
        public decimal? TotalPrice { get; set; }

        public string Status { get; set; }
        public List<BookingServiceDTO> Services { get; set; }
        
        // Thông tin người được đặt hộ (nếu có)
        public string? GuestFullName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestIdNumber { get; set; }
        
        // Check-in token và thời gian
        public string? CheckInToken { get; set; }
        public DateTime? CheckInTokenGeneratedAt { get; set; }
        public DateTime? CheckInTokenUsedAt { get; set; }
    }

    public class BookingServiceDTO
    {
        public string ServiceName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
