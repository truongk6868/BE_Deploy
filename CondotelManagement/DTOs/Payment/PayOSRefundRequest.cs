namespace CondotelManagement.DTOs.Payment
{
    public class PayOSRefundRequest
    {
        public long OriginalOrderCode { get; set; }
        public int RefundAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CustomerAccountNumber { get; set; } = string.Empty;
        public string CustomerBankCode { get; set; } = string.Empty;
    }

    public class PayOSRefundResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public PayOSRefundData? Data { get; set; }
    }

    public class PayOSRefundData
    {
        public long RefundOrderCode { get; set; }
        public string PaymentLinkId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public int Amount { get; set; }
    }
}






