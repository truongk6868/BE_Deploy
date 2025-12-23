using CondotelManagement.DTOs;
using CondotelManagement.Models;

namespace CondotelManagement.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationDTO>> GetAllAsync();
        Task<LocationDTO?> GetByIdAsync(int id);
        Task<LocationDTO> CreateAsync(LocationCreateUpdateDTO dto);
        Task<bool> UpdateAsync(int id, LocationCreateUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
    }
}
