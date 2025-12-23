namespace CondotelManagement.DTOs.Admin
{
    public class RefundRequestDTO
    {
        public int Id { get; set; }
        public string BookingId { get; set; } = string.Empty; // Format: BOOK-{id}
        public string CustomerName { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public BankInfoDTO BankInfo { get; set; } = new();
        public string Status { get; set; } = string.Empty; // "Pending" | "Completed" | "Refunded" | "Rejected"
        public string CancelDate { get; set; } = string.Empty; // Format: dd/MM/yyyy
    }

    public class BankInfoDTO
    {
        public string BankName { get; set; } = string.Empty; // Mã ngân hàng (MB, VCB, etc.)
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
    }
}
