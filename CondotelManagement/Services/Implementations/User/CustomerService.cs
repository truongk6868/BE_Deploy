using CondotelManagement.DTOs;
using CondotelManagement.Repositories;

namespace CondotelManagement.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _userRepo;

        public CustomerService(ICustomerRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<List<CustomerBookingDTO>> GetCustomerBookingsByHostAsync(int hostId)
        {
            return await _userRepo.GetCustomersByHostIdAsync(hostId);
        }
    }
}
