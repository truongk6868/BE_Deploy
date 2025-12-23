using CondotelManagement.Models;
using AmenityModel = CondotelManagement.Models.Amenity;

namespace CondotelManagement.Repositories.Interfaces.Amenity
{
    public interface IAmenityRepository
    {
        Task<IEnumerable<AmenityModel>> GetAllAsync();
		Task<IEnumerable<AmenityModel>> GetAllAsync(int hostId);
		Task<AmenityModel?> GetByIdAsync(int id);
        Task<AmenityModel> CreateAsync(AmenityModel amenity);
        Task<bool> UpdateAsync(AmenityModel amenity);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}


