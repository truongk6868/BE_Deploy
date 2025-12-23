using static CondotelManagement.Services.Implementations.Shared.EmailService;

namespace CondotelManagement.Services.Interfaces.Shared
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendPasswordResetOtpAsync(string toEmail, string otp);
        Task SendVerificationOtpAsync(string toEmail, string otp);
        Task SendRefundConfirmationEmailAsync(string toEmail, string customerName, int bookingId, decimal refundAmount, string? bankCode = null, string? accountNumber = null);
        Task SendRefundRejectionEmailAsync(string toEmail, string customerName, int bookingId, decimal refundAmount, string reason);
        Task SendPayoutConfirmationEmailAsync(string toEmail, string hostName, int bookingId, string condotelName, decimal amount, DateTime paidAt, string? bankName = null, string? accountNumber = null, string? accountHolderName = null);
        Task SendPayoutAccountErrorEmailAsync(string toEmail, string hostName, int bookingId, string condotelName, decimal amount, string? currentBankName = null, string? currentAccountNumber = null, string? currentAccountHolderName = null, string? errorMessage = null);
        Task SendPayoutRejectionEmailAsync(string toEmail, string hostName, int bookingId, string condotelName, decimal amount, string reason);
        Task SendVoucherNotificationEmailAsync(string toEmail, string customerName, int bookingId, List<VoucherInfo> vouchers);
        Task SendEmailAsync(string toEmail, string subject, string body);

        Task SendBookingConfirmedEmailAsync(
     string toEmail,
     BookingEmailInfo info
 );


        Task SendBookingConfirmationEmailAsync(string toEmail, string customerName, int bookingId, string condotelName, DateOnly checkInDate, DateOnly checkOutDate, decimal totalAmount, DateTime confirmedAt, string? checkInToken = null, string? guestFullName = null, string? guestPhone = null, string? guestIdNumber = null);
        Task SendNewBookingNotificationToHostAsync(string toEmail, string hostName, int bookingId, string condotelName, string customerName, DateOnly checkInDate, DateOnly checkOutDate, decimal totalAmount, DateTime confirmedAt);
    }

    public class VoucherInfo
    {
        public string Code { get; set; } = null!;
        public string CondotelName { get; set; } = null!;
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}
