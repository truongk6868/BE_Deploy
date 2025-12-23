using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs
{
	public class UtilityRequestDTO
	{

		[Required(ErrorMessage = "Tên tiện ích không được để trống.")]
		[MaxLength(100, ErrorMessage = "Tên tiện ích không được vượt quá 100 ký tự.")]
		public string Name { get; set; } = null!;

		[MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự.")]
		public string? Description { get; set; }

		[MaxLength(50, ErrorMessage = "Danh mục không được vượt quá 50 ký tự.")]
		public string? Category { get; set; }
	}
}
