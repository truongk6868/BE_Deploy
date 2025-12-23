using CondotelManagement.Data;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CondotelManagement.Repositories
{
    public class ServicePackageRepository : IServicePackageRepository
    {
        private readonly CondotelDbVer1Context _context;

        public ServicePackageRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }
		public async Task<IEnumerable<ServicePackage>> GetAllByHostAsync(int hostId)
		{
			return await _context.ServicePackages
				.Where(s => s.HostID == hostId && s.Status == "Active")
				.ToListAsync();
		}

		public async Task<ServicePackage?> GetByIdAsync(int id)
        {
            return await _context.ServicePackages.FindAsync(id);
        }

        public async Task AddAsync(ServicePackage entity)
        {
            await _context.ServicePackages.AddAsync(entity);
        }

        public async Task UpdateAsync(ServicePackage entity)
        {
            _context.ServicePackages.Update(entity);
            await Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
