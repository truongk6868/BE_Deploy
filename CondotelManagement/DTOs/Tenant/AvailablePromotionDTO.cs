namespace CondotelManagement.DTOs.Tenant
{
    public class AvailablePromotionDTO
    {
        public int PromotionId { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string TargetAudience { get; set; }
        public bool IsApplicable { get; set; } // Có áp dụng cho user hiện tại không
        public int DaysRemaining { get; set; }
    }

}
