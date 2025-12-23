using CondotelManagement.DTOs.Admin;

namespace CondotelManagement.Repositories.Interfaces.Admin
{
    public interface IAdminDashboardRepository
    {
        Task<AdminOverviewDto> GetOverviewAsync();
        Task<List<RevenueChartDto>> GetRevenueChartAsync();
        Task<List<TopCondotelDto>> GetTopCondotelsAsync();
        Task<List<TenantAnalyticsDto>> GetTenantAnalyticsAsync();
    }
}
