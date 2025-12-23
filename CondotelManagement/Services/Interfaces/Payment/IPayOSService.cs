using CondotelManagement.DTOs.Payment;

namespace CondotelManagement.Services.Interfaces.Payment
{
    public interface IPayOSService
    {
        Task<PayOSCreatePaymentResponse> CreatePaymentLinkAsync(PayOSCreatePaymentRequest request);
        Task<PayOSPaymentInfo> GetPaymentInfoAsync(string paymentLinkId);
        Task<PayOSPaymentInfo?> GetPaymentInfoByOrderCodeAsync(long orderCode);
        Task<PayOSCreatePaymentResponse> CancelPaymentLinkAsync(string paymentLinkId, string? cancellationReason = null);
        Task<PayOSCreatePaymentResponse> CancelPaymentLinkByOrderCodeAsync(long orderCode, string? cancellationReason = null);
        bool VerifyWebhookSignature(string signature, string body);
        Task<bool> ProcessWebhookAsync(PayOSWebhookData webhookData);
        
        // Refund: Tạo payment link mới cho customer để nhận tiền hoàn lại
        Task<PayOSCreatePaymentResponse> CreateRefundPaymentLinkAsync(int bookingId, decimal refundAmount, string customerName, string? customerEmail = null, string? customerPhone = null);
        
        // Refund Package: Tạo payment link mới cho host để nhận tiền hoàn lại package
        Task<PayOSCreatePaymentResponse> CreatePackageRefundPaymentLinkAsync(int hostId, long originalOrderCode, decimal refundAmount, string hostName, string? hostEmail = null, string? hostPhone = null);
    }
}

