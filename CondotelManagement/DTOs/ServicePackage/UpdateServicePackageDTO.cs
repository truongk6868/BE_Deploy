using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs
{
    public class UpdateServicePackageDTO
    {
		[Required(ErrorMessage = "Tên gói dịch vụ không được để trống.")]
		[MaxLength(100, ErrorMessage = "Tên gói dịch vụ không được vượt quá 100 ký tự.")]
		public string Name { get; set; }

		[MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự.")]
		public string? Description { get; set; }

		[Range(typeof(decimal), "0.01", "9999999999.99",
			ErrorMessage = "Giá gói dịch vụ phải từ 0.01 đến 9,999,999,999.99.")]
		public decimal Price { get; set; }
		[Required(ErrorMessage = "Trạng thái không được để trống.")]
		[RegularExpression("^(Active|Inactive)$",
	ErrorMessage = "Trạng thái không hợp lệ. Giá trị hợp lệ: Active, Inactive.")]
		public string Status { get; set; }
	}
}
