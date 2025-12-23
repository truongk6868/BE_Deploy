using CondotelManagement.Data;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories
{
    public class ResortRepository : IResortRepository
    {
        private readonly CondotelDbVer1Context _context;

        public ResortRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Resort>> GetAllAsync()
        {
            return await _context.Resorts
                .Include(r => r.Location)
                .ToListAsync();
        }

        public async Task<Resort?> GetByIdAsync(int id)
        {
            return await _context.Resorts
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.ResortId == id);
        }

        public async Task<IEnumerable<Resort>> GetByLocationIdAsync(int locationId)
        {
            return await _context.Resorts
                .Include(r => r.Location)
                .Where(r => r.LocationId == locationId)
                .ToListAsync();
        }

        public async Task<Resort> AddAsync(Resort resort)
        {
            _context.Resorts.Add(resort);
            await _context.SaveChangesAsync();
            return resort;
        }

        public async Task UpdateAsync(Resort resort)
        {
            _context.Resorts.Update(resort);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Resort resort)
        {
            _context.Resorts.Remove(resort);
            await _context.SaveChangesAsync();
        }
    }
}










