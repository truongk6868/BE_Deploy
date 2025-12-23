using CondotelManagement.DTOs.Booking;
using CondotelManagement.Models;
using System;

namespace CondotelManagement.DTOs
{
    public class BookingDTO
    {
        public int BookingId { get; set; }
        public int CondotelId { get; set; }
        public string CondotelName { get; set; }
        public string? ThumbnailImage { get; set; }  
        public int CustomerId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateTime CheckInAt { get; set; }
        public DateTime CheckOutAt { get; set; }
        public string? GuestFullName { get; set; }
        public string? GuestIdNumber { get; set; }
        public string? GuestPhone { get; set; }
        public decimal? TotalPrice { get; set; }
        public string Status { get; set; }
        public int? PromotionId { get; set; }
        public string? VoucherCode { get; set; } // Mã voucher để áp dụng
        public int? VoucherId { get; set; } // ID voucher đã áp dụng
        public List<ServicePackageSelectionDTO>? ServicePackages { get; set; } // Danh sách service packages được chọn
        public DateTime CreatedAt { get; set; }
        public bool CanReview { get; set; }
        public bool HasReviewed { get; set; }
        public bool CanRefund { get; set; }
        
        // Trạng thái hoàn tiền cho booking hủy
        // null = Chưa có refund request (cancel payment - chưa thanh toán)
        // "Pending" = Đang chờ hoàn tiền
        // "Refunded" = Đã hoàn tiền thành công (qua PayOS)
        // "Completed" = Đã hoàn tiền thủ công (admin xác nhận)
        public string? RefundStatus { get; set; }
        
        // Check-in token và thời gian
        public string? CheckInToken { get; set; }
        public DateTime? CheckInTokenGeneratedAt { get; set; }
        public DateTime? CheckInTokenUsedAt { get; set; }
    }
}
