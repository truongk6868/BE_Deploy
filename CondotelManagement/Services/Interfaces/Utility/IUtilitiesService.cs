using CondotelManagement.DTOs;
using CondotelManagement.Models;

namespace CondotelManagement.Services
{
	public interface IUtilitiesService
	{
		Task<IEnumerable<UtilityResponseDTO>> GetAllAsync();
		Task<UtilityResponseDTO?> GetByIdAsync(int id);
		Task<UtilityResponseDTO> CreateAsync(UtilityRequestDTO dto);
		Task<bool> UpdateAsync(int id, UtilityRequestDTO dto);
		Task<bool> DeleteAsync(int id);
		Task<IEnumerable<UtilityResponseDTO>> GetByResortAsync(int resortId);
	}
}
