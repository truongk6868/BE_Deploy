using CondotelManagement.DTOs.Admin;

namespace CondotelManagement.Services.Interfaces.Admin
{
    public interface IAdminDashboardService
    {
        Task<AdminOverviewDto> GetOverviewAsync();
        Task<List<RevenueChartDto>> GetRevenueChartAsync();
        Task<List<TopCondotelDto>> GetTopCondotelsAsync();
        Task<List<TenantAnalyticsDto>> GetTenantAnalyticsAsync();
    }
}
