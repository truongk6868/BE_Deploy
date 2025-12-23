using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs
{
	public class HostVoucherSettingDTO
	{
		[Required(ErrorMessage = "DiscountPercentage không được trống.")]
		[Range(0, 100, ErrorMessage = "DiscountPercentage phải từ 0 đến 100%.")]
		public decimal? DiscountPercentage { get; set; }
		[Required(ErrorMessage = "DiscountAmount không được trống.")]
		[Range(0, 100000000, ErrorMessage = "DiscountAmount không hợp lệ.")]
		public decimal? DiscountAmount { get; set; }

		public bool AutoGenerate { get; set; }

		[Range(1, 1000, ErrorMessage = "UsageLimit phải từ 1 đến 1000.")]
		public int UsageLimit { get; set; }

		[Range(1, 24, ErrorMessage = "ValidMonths phải từ 1 đến 24 tháng.")]
		public int ValidMonths { get; set; }
	}
}
