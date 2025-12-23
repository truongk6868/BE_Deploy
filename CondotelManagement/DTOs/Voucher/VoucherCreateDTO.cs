using System.ComponentModel.DataAnnotations;
using CondotelManagement.Helpers;

namespace CondotelManagement.DTOs
{
	[DateRangeValidation(
		StartDatePropertyName = "StartDate",
		EndDatePropertyName = "EndDate",
		ErrorMessage = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.")]
	public class VoucherCreateDTO
	{
		public int? CondotelID { get; set; }
		public int? UserID { get; set; }

		[Required(ErrorMessage = "Mã voucher không được để trống.")]
		[StringLength(50, ErrorMessage = "Code không được vượt quá 50 ký tự.")]
		public string Code { get; set; } = null!;

		[Range(0.01, 99999999.99, ErrorMessage = "DiscountAmount phải > 0.")]
		public decimal? DiscountAmount { get; set; }

		[Range(0.01, 999.99, ErrorMessage = "DiscountPercentage phải > 0 và < 1000.")]
		public decimal? DiscountPercentage { get; set; }

		[Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
		public DateOnly StartDate { get; set; }

		[Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
		public DateOnly EndDate { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "UsageLimit phải >= 1.")]
		public int? UsageLimit { get; set; }
	}
}
