using CondotelManagement.DTOs.Host;

namespace CondotelManagement.Services.Interfaces.Host
{
    public interface IHostPayoutService
    {
        Task<HostPayoutResponseDTO> ProcessPayoutsAsync();
        Task<HostPayoutResponseDTO> ProcessPayoutForBookingAsync(int bookingId);
        Task<HostPayoutResponseDTO> ReportAccountErrorAsync(int bookingId, string errorMessage);
        Task<HostPayoutResponseDTO> RejectPayoutAsync(int bookingId, string reason);
        Task<List<HostPayoutItemDTO>> GetPendingPayoutsAsync(int? hostId = null);
        Task<List<HostPayoutItemDTO>> GetPaidPayoutsAsync(int? hostId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<HostPayoutItemDTO>> GetRejectedPayoutsAsync(int? hostId = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}


