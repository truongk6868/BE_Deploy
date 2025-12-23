using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Amenity;

namespace CondotelManagement.Services.Interfaces.Amenity
{
    public interface IAmenityService
    {
		Task<IEnumerable<AmenityResponseDTO>> GetAllAsync();
		Task<IEnumerable<AmenityResponseDTO>> GetAllAsync(int hostId);
        Task<AmenityResponseDTO?> GetByIdAsync(int id);
        Task<AmenityResponseDTO> CreateAsync(int hostId, AmenityRequestDTO dto);
        Task<bool> UpdateAsync(int id, AmenityRequestDTO dto);
        Task<bool> DeleteAsync(int id);

	}
}

