using CondotelManagement.DTOs;

namespace CondotelManagement.Services
{
    public interface IHostReportService
    {
        Task<HostReportDTO> GetReport(int hostId, DateOnly? from, DateOnly? to);
        Task<RevenueReportResponseDTO> GetRevenueReportByMonthYear(int hostId, int? year, int? month);
    }
}
