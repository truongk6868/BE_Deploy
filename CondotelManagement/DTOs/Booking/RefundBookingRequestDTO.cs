namespace CondotelManagement.DTOs.Booking
{
    public class RefundBookingRequestDTO
    {
        public string? BankCode { get; set; } // Mã ngân hàng (MB, VCB, TCB, etc.)
        public string? AccountNumber { get; set; }
        public string? AccountHolder { get; set; } // Tên chủ tài khoản
    }
}





