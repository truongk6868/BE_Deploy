namespace CondotelManagement.DTOs
{
    public class PromotionDTO
    {
        public int PromotionId { get; set; }
        public string Name { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string? TargetAudience { get; set; }
        public string Status { get; set; } = null!;
        public int? CondotelId { get; set; }
        public string? CondotelName { get; set; }
    }
}








