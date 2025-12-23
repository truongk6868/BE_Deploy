using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Admin;
using CondotelManagement.DTOs.Booking;

namespace CondotelManagement.Services.Interfaces.BookingService
{
    public interface IBookingService
    {
        Task<IEnumerable<BookingDTO>> GetBookingsByCustomerAsync(int customerId);
        Task<BookingDTO?> GetBookingByIdAsync(int id);
        [Obsolete("Use GetBookingByIdAsync instead")]
        BookingDTO GetBookingById(int id);
        Task<ServiceResultDTO> CreateBookingAsync(CreateBookingDTO booking, int customerId);

        BookingDTO UpdateBooking(BookingDTO booking);
        Task<bool> CancelBooking(int bookingId, int customerId);
        Task<bool> CancelPayment(int bookingId, int customerId);
        Task<ServiceResultDTO> RefundBooking(int bookingId, int customerId, string? bankCode = null, string? accountNumber = null, string? accountHolder = null);
        Task<bool> CanRefundBooking(int bookingId, int customerId);
        Task<ServiceResultDTO> AdminRefundBooking(int bookingId, string? reason = null);

        bool CheckAvailability(int roomId, DateOnly checkIn, DateOnly checkOut);

        IEnumerable<HostBookingDTO> GetBookingsByHost(int hostId);
        IEnumerable<HostBookingDTO> GetBookingsByHost(int hostId, DTOs.Booking.BookingFilterDTO? filter);
        IEnumerable<HostBookingDTO> GetBookingsByHostAndCustomer(int hostId, int customerId);

        // Admin refund management
        Task<List<RefundRequestDTO>> GetRefundRequestsAsync(string? searchTerm = null, string? status = "all", DateTime? startDate = null, DateTime? endDate = null, int? condotelTypeId = null);
        Task<ServiceResultDTO> ConfirmRefundManually(int bookingId);
        Task<ServiceResultDTO> RejectRefundRequest(int refundRequestId, string reason);
    }
}
