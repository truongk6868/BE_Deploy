namespace CondotelManagement.DTOs.Package
{
	public class CancelPackageRequestDTO
	{
		public string? Reason { get; set; }
	}

	public class CancelPackageResponseDTO
	{
		public bool Success { get; set; }
		public string Message { get; set; } = null!;
		public decimal? RefundAmount { get; set; }
		public string? RefundPaymentLink { get; set; }
		public int? DaysUsed { get; set; }
		public int? DaysRemaining { get; set; }
	}
}


