using CondotelManagement.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CondotelManagement.Services.Interfaces.Admin;


namespace CondotelManagement.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/dashboard")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var result = await _dashboardService.GetOverviewAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));

        }

        [HttpGet("revenue/chart")]
        public async Task<IActionResult> GetRevenueChart()
        {
            var result = await _dashboardService.GetRevenueChartAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));

        }

        [HttpGet("top-condotels")]
        public async Task<IActionResult> GetTopCondotels()
        {
            var result = await _dashboardService.GetTopCondotelsAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));

        }

        [HttpGet("tenant-analytics")]
        public async Task<IActionResult> GetTenantAnalytics()
        {
            var result = await _dashboardService.GetTenantAnalyticsAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }
    }
}
