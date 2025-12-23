using CondotelManagement.Data;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Amenity;
using Microsoft.EntityFrameworkCore;
using AmenityModel = CondotelManagement.Models.Amenity;

namespace CondotelManagement.Repositories.Implementations.Amenity
{
    public class AmenityRepository : IAmenityRepository
    {
        private readonly CondotelDbVer1Context _context;

        public AmenityRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AmenityModel>> GetAllAsync()
        {
            return await _context.Amenities
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<AmenityModel?> GetByIdAsync(int id)
        {
            return await _context.Amenities
                .FirstOrDefaultAsync(a => a.AmenityId == id);
        }

        public async Task<AmenityModel> CreateAsync(AmenityModel amenity)
        {
            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync();
            return amenity;
        }

        public async Task<bool> UpdateAsync(AmenityModel amenity)
        {
            var existing = await _context.Amenities
                .FirstOrDefaultAsync(a => a.AmenityId == amenity.AmenityId);

            if (existing == null)
                return false;

            existing.Name = amenity.Name;
            existing.Description = amenity.Description;
            existing.Category = amenity.Category;

            _context.Amenities.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var amenity = await _context.Amenities
                .Include(a => a.CondotelAmenities)
                .FirstOrDefaultAsync(a => a.AmenityId == id);

            if (amenity == null)
                return false;

            // Kiểm tra xem amenity có đang được sử dụng không
            if (amenity.CondotelAmenities != null && amenity.CondotelAmenities.Any())
            {
                throw new InvalidOperationException("Không thể xóa tiện ích đang được sử dụng bởi các căn hộ khách sạn.");
            }

            _context.Amenities.Remove(amenity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Amenities.AnyAsync(a => a.AmenityId == id);
        }

		public async Task<IEnumerable<AmenityModel>> GetAllAsync(int hostId)
		{
			return await _context.Amenities
                .Where(a => a.HostID == hostId)
				.OrderBy(a => a.Name)
				.ToListAsync();
		}
	}
}


