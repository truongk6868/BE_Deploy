using CondotelManagement.DTOs.Amenity;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Amenity;
using CondotelManagement.Services.Interfaces.Amenity;
using AmenityModel = CondotelManagement.Models.Amenity;

namespace CondotelManagement.Services.Implementations.Amenity
{
    public class AmenityService : IAmenityService
    {
        private readonly IAmenityRepository _repository;

        public AmenityService(IAmenityRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<AmenityResponseDTO>> GetAllAsync()
        {
            var amenities = await _repository.GetAllAsync();
            return amenities.Select(a => new AmenityResponseDTO
            {
                AmenityId = a.AmenityId,
                Name = a.Name,
                Description = a.Description,
                Category = a.Category
            });
        }

        public async Task<AmenityResponseDTO?> GetByIdAsync(int id)
        {
            var amenity = await _repository.GetByIdAsync(id);
            if (amenity == null)
                return null;

            return new AmenityResponseDTO
            {
                AmenityId = amenity.AmenityId,
                Name = amenity.Name,
                Description = amenity.Description,
                Category = amenity.Category
            };
        }

        public async Task<AmenityResponseDTO> CreateAsync(int hostId, AmenityRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Amenity name is required", nameof(dto.Name));

            var amenity = new AmenityModel
            {
                HostID = hostId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Category = dto.Category?.Trim()
            };

            var created = await _repository.CreateAsync(amenity);

            return new AmenityResponseDTO
            {
                AmenityId = created.AmenityId,
                Name = created.Name,
                Description = created.Description,
                Category = created.Category
            };
        }

        public async Task<bool> UpdateAsync(int id, AmenityRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Amenity name is required", nameof(dto.Name));

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return false;

            existing.Name = dto.Name.Trim();
            existing.Description = dto.Description?.Trim();
            existing.Category = dto.Category?.Trim();

            return await _repository.UpdateAsync(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (!await _repository.ExistsAsync(id))
                return false;

            return await _repository.DeleteAsync(id);
        }

		public async Task<IEnumerable<AmenityResponseDTO>> GetAllAsync(int hostId)
		{
			var amenities = await _repository.GetAllAsync(hostId);
			return amenities.Select(a => new AmenityResponseDTO
			{
				AmenityId = a.AmenityId,
				Name = a.Name,
				Description = a.Description,
				Category = a.Category
			});
		}
	}
}


