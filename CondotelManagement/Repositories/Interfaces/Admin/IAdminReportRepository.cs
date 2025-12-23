using CondotelManagement.DTOs.Admin;
using CondotelManagement.Models;

namespace CondotelManagement.Repositories.Interfaces.Admin
{
    public interface IAdminReportRepository
    {
        Task<AdminReport> CreateReportAsync(AdminReport report);
        Task<AdminReport?> GetReportByIdAsync(int reportId);
        Task<IEnumerable<AdminReport>> GetAllReportsAsync();
        Task<IEnumerable<AdminReport>> GetReportsByAdminIdAsync(int adminId);
        Task<IEnumerable<AdminReport>> GetReportsByHostIdAsync(int hostId);
        Task<bool> DeleteReportAsync(int reportId);
        Task<AdminReport> UpdateReportAsync(AdminReport report);
    }
}




