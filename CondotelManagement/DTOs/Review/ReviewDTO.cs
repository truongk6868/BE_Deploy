using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs
{
	public class ReviewDTO
	{
		public int ReviewId { get; set; }
		public int CondotelId { get; set; }
		public int? BookingId { get; set; }
		public string CondotelName { get; set; } = null!;
		public int UserId { get; set; }
		public string UserName { get; set; } = null!;
		public string? UserImageUrl { get; set; } // Avatar của user
		public int Rating { get; set; }
		public string Comment { get; set; } = null!;
		public string? Reply { get; set; }
		public string Status { get; set; } = "Visible";
		public DateTime CreatedAt { get; set; }
	}

	public class ReviewReplyDTO
	{
		[Required(ErrorMessage = "Nội dung phản hồi không được để trống.")]
		[MaxLength(1000, ErrorMessage = "Nội dung phản hồi không được vượt quá 1000 ký tự.")]
		public string Reply { get; set; } = null!;
	}
}
