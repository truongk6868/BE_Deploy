namespace CondotelManagement.DTOs.Host
{
    public class HostPayoutResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<HostPayoutItemDTO> ProcessedItems { get; set; } = new List<HostPayoutItemDTO>();
    }

    public class HostPayoutItemDTO
    {
        public int BookingId { get; set; }
        public int CondotelId { get; set; }
        public string CondotelName { get; set; } = string.Empty;
        public int HostId { get; set; }
        public string HostName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateOnly EndDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public bool IsPaid { get; set; }
        public int DaysSinceCompleted { get; set; }
        
        // Thông tin khách hàng
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "Khách hàng"; // Giá trị mặc định để tránh undefined
        public string? CustomerEmail { get; set; }
        
        // Thông tin tài khoản ngân hàng của host (để thực hiện thanh toán cho host)
        // Lấy từ Wallet của host
        public string? BankName { get; set; } // Tên ngân hàng của host
        public string? AccountNumber { get; set; } // Số tài khoản ngân hàng của host
        public string? AccountHolderName { get; set; } // Tên chủ tài khoản của host
        
        // Thông tin từ chối (nếu có)
        public DateTime? RejectedAt { get; set; } // Ngày giờ từ chối
        public string? RejectionReason { get; set; } // Lý do từ chối
    }
}


