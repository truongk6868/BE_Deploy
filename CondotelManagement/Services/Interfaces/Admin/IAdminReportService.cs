using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Admin;

namespace CondotelManagement.Services.Interfaces.Admin
{
    public interface IAdminReportService
    {
        Task<AdminReportResponseDTO> CreateHostReportAsync(int adminId, AdminReportCreateDTO dto);
        Task<AdminReportResponseDTO?> GetReportByIdAsync(int reportId);
        Task<IEnumerable<AdminReportListDTO>> GetAllReportsAsync();
        Task<IEnumerable<AdminReportListDTO>> GetReportsByAdminIdAsync(int adminId);
        Task<IEnumerable<AdminReportListDTO>> GetReportsByHostIdAsync(int hostId);
        Task<bool> DeleteReportAsync(int reportId);
        Task<byte[]?> GenerateExcelReportAsync(int? hostId, DateOnly? from, DateOnly? to, int? year, int? month);
        Task<RevenueReportResponseDTO> GetAllHostsRevenueReportAsync(int? year, int? month);
        Task<IEnumerable<HostListItemDTO>> GetAllHostsAsync();
    }
}

