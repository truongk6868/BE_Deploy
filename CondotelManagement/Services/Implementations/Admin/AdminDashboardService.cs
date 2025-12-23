using CondotelManagement.DTOs.Admin;
using CondotelManagement.Repositories.Interfaces.Admin;
using CondotelManagement.Services.Interfaces.Admin;

namespace CondotelManagement.Services.Implementations.Admin
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IAdminDashboardRepository _repo;

        public AdminDashboardService(IAdminDashboardRepository repo)
        {
            _repo = repo;
        }

        public async Task<AdminOverviewDto> GetOverviewAsync() => await _repo.GetOverviewAsync();

        public async Task<List<RevenueChartDto>> GetRevenueChartAsync() => await _repo.GetRevenueChartAsync();

        public async Task<List<TopCondotelDto>> GetTopCondotelsAsync() => await _repo.GetTopCondotelsAsync();

        public async Task<List<TenantAnalyticsDto>> GetTenantAnalyticsAsync() => await _repo.GetTenantAnalyticsAsync();
    }
}
