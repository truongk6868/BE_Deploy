namespace CondotelManagement.DTOs
{
	public class VoucherDTO
	{
		public int VoucherID { get; set; }
		public int? CondotelID { get; set; }
		public string? CondotelName { get; set; }
		public int? UserID { get; set; }
		public string? FullName { get; set; }
		public string Code { get; set; } = null!;
		public decimal? DiscountAmount { get; set; }
		public decimal? DiscountPercentage { get; set; }
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public string Status { get; set; } = "Active";
	}
}
