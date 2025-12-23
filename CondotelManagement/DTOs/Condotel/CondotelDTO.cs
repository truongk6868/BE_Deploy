namespace CondotelManagement.DTOs
{
    public class CondotelDTO
    {
        public int CondotelId { get; set; }
        public string Name { get; set; }
        public decimal PricePerNight { get; set; }
        public int Beds { get; set; }
        public int Bathrooms { get; set; }
        public string Status { get; set; }

        //hiển thị ảnh đầu tiên
        public string? ThumbnailUrl { get; set; }

        //tên resort hoặc host
        public string? ResortName { get; set; }
        public string? HostName { get; set; }

        // Review info
        public int ReviewCount { get; set; }
        public double ReviewRate { get; set; }

        // Promotion đang active (nếu có)
        public PromotionDTO? ActivePromotion { get; set; }
		public CondotelPriceDTO? ActivePrice { get; set; }
	}
}
