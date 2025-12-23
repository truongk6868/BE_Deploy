using CondotelManagement.DTOs;
using CondotelManagement.Repositories;

namespace CondotelManagement.Services
{
    public class HostReportService : IHostReportService
    {
        private readonly IHostReportRepository _repo;

        public HostReportService(IHostReportRepository repo)
        {
            _repo = repo;
        }
        public async Task<HostReportDTO> GetReport(int hostId, DateOnly? from, DateOnly? to)
        {
            return await _repo.GetHostReportAsync(hostId, from, to);
        }

        public async Task<RevenueReportResponseDTO> GetRevenueReportByMonthYear(int hostId, int? year, int? month)
        {
            return await _repo.GetRevenueReportByMonthYearAsync(hostId, year, month);
        }
    }
}
