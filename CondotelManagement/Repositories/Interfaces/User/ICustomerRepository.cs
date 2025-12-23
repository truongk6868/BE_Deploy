using CondotelManagement.DTOs;
using CondotelManagement.Models;

namespace CondotelManagement.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<CustomerBookingDTO>> GetCustomersByHostIdAsync(int hostId);
    }
}
