namespace CondotelManagement.DTOs.Booking
{
    public class BookingFilterDTO
    {
        /// <summary>
        /// Tìm kiếm theo Booking ID, tên khách hàng, tên condotel
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Lọc theo status: Pending, Confirmed, Completed, Cancelled, hoặc "all" để lấy tất cả
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Lọc theo condotel ID
        /// </summary>
        public int? CondotelId { get; set; }

        /// <summary>
        /// Lọc theo ngày đặt phòng (từ ngày)
        /// </summary>
        public DateTime? BookingDateFrom { get; set; }

        /// <summary>
        /// Lọc theo ngày đặt phòng (đến ngày)
        /// </summary>
        public DateTime? BookingDateTo { get; set; }

        /// <summary>
        /// Lọc theo ngày check-in (từ ngày)
        /// </summary>
        public DateOnly? StartDateFrom { get; set; }

        /// <summary>
        /// Lọc theo ngày check-in (đến ngày)
        /// </summary>
        public DateOnly? StartDateTo { get; set; }

        /// <summary>
        /// Lọc theo ngày check-out (từ ngày)
        /// </summary>
        public DateOnly? EndDateFrom { get; set; }

        /// <summary>
        /// Lọc theo ngày check-out (đến ngày)
        /// </summary>
        public DateOnly? EndDateTo { get; set; }

        /// <summary>
        /// Sắp xếp theo: "bookingDate", "startDate", "endDate", "totalPrice" (mặc định: "bookingDate")
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sắp xếp giảm dần (true) hay tăng dần (false), mặc định: true (mới nhất trước)
        /// </summary>
        public bool? SortDescending { get; set; } = true;
    }
}






