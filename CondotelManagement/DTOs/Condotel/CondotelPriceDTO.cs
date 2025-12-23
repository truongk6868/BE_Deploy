namespace CondotelManagement.DTOs
{
	public class CondotelPriceDTO
	{
		public int PriceId { get; set; }

		public DateOnly StartDate { get; set; }

		public DateOnly EndDate { get; set; }

		public decimal BasePrice { get; set; }

		public string PriceType { get; set; } = null!;

		public string? Description { get; set; }
	}
}