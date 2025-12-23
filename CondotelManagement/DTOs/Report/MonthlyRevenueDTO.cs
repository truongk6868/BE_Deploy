namespace CondotelManagement.DTOs
{
	public class MonthlyRevenueDTO
	{
		public int Year { get; set; }
		public int Month { get; set; }
		public string MonthName { get; set; } = null!; // "Tháng 1", "Tháng 2", etc.
		public decimal Revenue { get; set; }
		public int TotalBookings { get; set; }
		public int CompletedBookings { get; set; }
		public int CancelledBookings { get; set; }
	}

	public class YearlyRevenueDTO
	{
		public int Year { get; set; }
		public decimal TotalRevenue { get; set; }
		public int TotalBookings { get; set; }
		public int CompletedBookings { get; set; }
		public int CancelledBookings { get; set; }
		public List<MonthlyRevenueDTO> MonthlyData { get; set; } = new List<MonthlyRevenueDTO>();
	}

	public class RevenueReportResponseDTO
	{
		public decimal TotalRevenue { get; set; }
		public int TotalBookings { get; set; }
		public int CompletedBookings { get; set; }
		public int CancelledBookings { get; set; }
		public List<MonthlyRevenueDTO> MonthlyRevenues { get; set; } = new List<MonthlyRevenueDTO>();
		public List<YearlyRevenueDTO> YearlyRevenues { get; set; } = new List<YearlyRevenueDTO>();
	}
}


