namespace CondotelManagement.DTOs
{
    public class HostReportDTO
    {
        public decimal Revenue { get; set; }
        public int TotalRooms { get; set; }
        public int RoomsBooked { get; set; }
        public double OccupancyRate { get; set; } // ví dụ: 45.5 (phần trăm)
        public int TotalBookings { get; set; }
        public int TotalCancellations { get; set; }
		public int CompletedBookings { get; set; }
		public int TotalCustomers { get; set; } // Tổng số khách hàng unique
		public decimal AverageBookingValue { get; set; } // Giá trị trung bình mỗi đặt phòng
		public int PendingBookings { get; set; } // Đang xử lý
		public int ConfirmedBookings { get; set; } // Đã xác nhận

	}
}
