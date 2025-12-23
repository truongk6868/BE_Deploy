using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Amenity
{
    public class AmenityRequestDTO
    {
		[Required(ErrorMessage = "Tên tiện ích không được bỏ trống.")]
		[StringLength(100, ErrorMessage = "Tên tiện ích không được vượt quá 100 ký tự.")]
		public string Name { get; set; } = null!;

		[StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự.")]
		public string? Description { get; set; }

		[StringLength(50, ErrorMessage = "Category không được vượt quá 50 ký tự.")]
		public string? Category { get; set; }
	}
}

