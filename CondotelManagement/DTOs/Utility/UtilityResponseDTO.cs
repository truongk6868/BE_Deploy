namespace CondotelManagement.DTOs
{
	public class UtilityResponseDTO
	{
		public int UtilityId { get; set; }

		public string Name { get; set; } = null!;

		public string? Description { get; set; }

		public string? Category { get; set; }
	}
}
