namespace CondotelManagement.DTOs
{
	public class HostVoucherSettingDetailDTO
	{
		public int SettingID { get; set; }
		public int HostID { get; set; }
		public decimal? DiscountPercentage { get; set; }
		public decimal? DiscountAmount { get; set; }
		public bool AutoGenerate { get; set; }
		public int UsageLimit { get; set; }
		public int ValidMonths { get; set; }
	}
}
