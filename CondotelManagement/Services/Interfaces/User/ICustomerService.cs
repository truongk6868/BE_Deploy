using CondotelManagement.DTOs;

namespace CondotelManagement.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerBookingDTO>> GetCustomerBookingsByHostAsync(int hostId);
    }
}
