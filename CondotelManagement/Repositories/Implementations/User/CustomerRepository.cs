using CondotelManagement.Data;
using CondotelManagement.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CondotelDbVer1Context _context;

        public CustomerRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public async Task<List<CustomerBookingDTO>> GetCustomersByHostIdAsync(int hostId)
        {
            return await _context.Bookings
            .Where(b => b.Condotel.HostId == hostId)
			.Select(b => b.Customer)
		    .Distinct()  // DISTINCT Customer
			.Select(b => new CustomerBookingDTO
            {
                UserId = b.UserId,
                FullName = b.FullName,
                Email = b.Email,
                Phone = b.Phone,
                Gender = b.Gender,
                DateOfBirth = b.DateOfBirth,
                Address = b.Address
            })
            .OrderByDescending(x => x.UserId)
            .ToListAsync();
        }
    }
}
