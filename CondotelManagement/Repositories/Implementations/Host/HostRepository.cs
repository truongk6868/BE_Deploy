using HostModel = CondotelManagement.Models.Host;
using CondotelManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories
{
    public class HostRepository : IHostRepository
    {
        private readonly CondotelDbVer1Context _context;

        public HostRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

		public async Task<HostModel> GetHostProfileAsync(int userId)
		{
			return await _context.Hosts
			.Include(h => h.User)
			.Include(h => h.Wallets)
			.Include(h => h.HostPackages)
				.ThenInclude(hp => hp.Package)
			.FirstOrDefaultAsync(h => h.UserId == userId);
		}

		HostModel IHostRepository.GetByUserId(int userId)
        {
            return _context.Hosts.FirstOrDefault(h => h.UserId == userId);
        }
		public async Task UpdateHostAsync(HostModel host)
		{
			_context.Hosts.Update(host);
			_context.Users.Update(host.User);
			// Wallets sẽ được update tự động khi update host
			await _context.SaveChangesAsync();
		}
	}
}
