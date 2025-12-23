using CondotelManagement.DTOs;
using CondotelManagement.Models;
using CondotelManagement.Repositories;

namespace CondotelManagement.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepo;

        public LocationService(ILocationRepository locationRepo)
        {
            _locationRepo = locationRepo;
        }

        public async Task<LocationDTO> CreateAsync(LocationCreateUpdateDTO dto)
        {
            var location = new Location
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl
            };
            var created = await _locationRepo.AddAsync(location);

            return new LocationDTO
            {
                LocationId = created.LocationId,
                Name = created.Name,
                Description = created.Description,
                ImageUrl = created.ImageUrl
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var location = await _locationRepo.GetByIdAsync(id);
            if (location == null) return false;

            await _locationRepo.DeleteAsync(location);
            return true;
        }

        public async Task<IEnumerable<LocationDTO>> GetAllAsync()
        {
            var locations = await _locationRepo.GetAllAsync();
            return locations.Select(l => new LocationDTO
            {
                LocationId = l.LocationId,
                Name = l.Name,
                Description = l.Description,
                ImageUrl = l.ImageUrl
            });
        }

        public async Task<LocationDTO?> GetByIdAsync(int id)
        {
            var l = await _locationRepo.GetByIdAsync(id);
            if (l == null) return null;

            return new LocationDTO
            {
                LocationId = l.LocationId,
                Name = l.Name,
                Description = l.Description,
                ImageUrl = l.ImageUrl
            };
        }

        public async Task<bool> UpdateAsync(int id, LocationCreateUpdateDTO dto)
        {
            var location = await _locationRepo.GetByIdAsync(id);
            if (location == null) return false;

            location.Name = dto.Name;
            location.Description = dto.Description;
            location.ImageUrl = dto.ImageUrl;

            await _locationRepo.UpdateAsync(location);
            return true;
        }
    }
}
