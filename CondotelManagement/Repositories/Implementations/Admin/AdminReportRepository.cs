using CondotelManagement.Data;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Admin;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories.Implementations.Admin
{
    public class AdminReportRepository : IAdminReportRepository
    {
        private readonly CondotelDbVer1Context _context;

        public AdminReportRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public async Task<AdminReport> CreateReportAsync(AdminReport report)
        {
            await _context.AdminReports.AddAsync(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<AdminReport?> GetReportByIdAsync(int reportId)
        {
            return await _context.AdminReports
                .Include(r => r.Admin)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<IEnumerable<AdminReport>> GetAllReportsAsync()
        {
            return await _context.AdminReports
                .Include(r => r.Admin)
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdminReport>> GetReportsByAdminIdAsync(int adminId)
        {
            return await _context.AdminReports
                .Include(r => r.Admin)
                .Where(r => r.AdminId == adminId)
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdminReport>> GetReportsByHostIdAsync(int hostId)
        {
            // Lưu hostId trong ReportType dạng "HostReport_{hostId}" hoặc trong FileUrl
            // Tìm các report có ReportType chứa hostId
            return await _context.AdminReports
                .Include(r => r.Admin)
                .Where(r => r.ReportType != null && r.ReportType.Contains($"HostReport") && r.ReportType.Contains(hostId.ToString()))
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();
        }

        public async Task<bool> DeleteReportAsync(int reportId)
        {
            var report = await _context.AdminReports.FindAsync(reportId);
            if (report == null) return false;

            _context.AdminReports.Remove(report);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AdminReport> UpdateReportAsync(AdminReport report)
        {
            _context.AdminReports.Update(report);
            await _context.SaveChangesAsync();
            return report;
        }
    }
}




