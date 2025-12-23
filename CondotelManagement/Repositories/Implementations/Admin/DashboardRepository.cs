using CondotelManagement.Data;
using CondotelManagement.DTOs.Admin;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Admin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CondotelManagement.Repositories.Implementations.Admin
{
    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly CondotelDbVer1Context _context;

        public AdminDashboardRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

        // Tổng quan: condotel, tenant, booking, revenue
        public async Task<AdminOverviewDto> GetOverviewAsync()
        {
            var totalCondotels = await _context.Condotels.CountAsync();

            // Tổng số tenant (user có role là Customer)
            var totalTenants = await _context.Users
                .Include(u => u.Role)
                .CountAsync(u => u.Role.RoleName == "Customer");

            // Tổng số booking
            var totalBookings = await _context.Bookings.CountAsync();

            // Tổng doanh thu (chỉ tính booking Completed)
            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == "Completed")
                .SumAsync(b => (decimal?)(b.TotalPrice ?? 0)) ?? 0;

            return new AdminOverviewDto
            {
                TotalCondotels = totalCondotels,
                TotalTenants = totalTenants,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue
            };
        }

        // Biểu đồ doanh thu theo tháng/năm
        public async Task<List<RevenueChartDto>> GetRevenueChartAsync()
        {
            var data = await _context.Bookings
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new RevenueChartDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(b => (decimal)(b.TotalPrice ?? 0))
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return data;
        }

        // Top 5 condotel doanh thu cao nhất
        public async Task<List<TopCondotelDto>> GetTopCondotelsAsync()
        {
            var data = await _context.Bookings
                .Include(b => b.Condotel)
                .GroupBy(b => b.Condotel.Name)
                .Select(g => new TopCondotelDto
                {
                    CondotelName = g.Key,
                    BookingCount = g.Count(),
                    TotalRevenue = g.Sum(b => (decimal)(b.TotalPrice ?? 0))
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToListAsync();

            return data;
        }

        // Phân tích khách thuê (customer)
        public async Task<List<TenantAnalyticsDto>> GetTenantAnalyticsAsync()
        {
            var data = await _context.Bookings
                .Include(b => b.Customer)
                .GroupBy(b => b.Customer.FullName)
                .Select(g => new TenantAnalyticsDto
                {
                    TenantName = g.Key,
                    BookingCount = g.Count(),
                    TotalSpent = g.Sum(b => (decimal)(b.TotalPrice ?? 0))
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToListAsync();

            return data;
        }
    }
}
