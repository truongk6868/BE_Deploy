namespace CondotelManagement.DTOs.Payment
{
    public class PayOSPaymentInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public PayOSPaymentInfoData? Data { get; set; }
    }

    public class PayOSPaymentInfoData
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; } = string.Empty;
        
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public int AmountPaid { get; set; }
        public int AmountRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PayOSTransaction> Transactions { get; set; } = new();
        public string CancelUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        
        // Helper property to get PaymentLinkId or Id as fallback
        public string GetPaymentLinkId() => !string.IsNullOrEmpty(PaymentLinkId) ? PaymentLinkId : Id;
    }

    public class PayOSTransaction
    {
        public string Reference { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDateTime { get; set; }
        public string VirtualAccountName { get; set; } = string.Empty;
        public string VirtualAccountNumber { get; set; } = string.Empty;
    }
}


