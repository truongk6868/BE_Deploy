namespace CondotelManagement.DTOs.Admin
{
    public class AdminOverviewDto
    {
        public int TotalCondotels { get; set; }
        public int TotalTenants { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueChartDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopCondotelDto
    {
        public string CondotelName { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TenantAnalyticsDto
    {
        public string TenantName { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
