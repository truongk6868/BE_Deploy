using CondotelManagement.DTOs;

namespace CondotelManagement.Services
{
    public interface IResortService
    {
        Task<IEnumerable<ResortDTO>> GetAllAsync();
        Task<ResortDTO?> GetByIdAsync(int id);
        Task<IEnumerable<ResortDTO>> GetByLocationIdAsync(int locationId);
        Task<ResortDTO> CreateAsync(ResortCreateUpdateDTO dto);
        Task<bool> UpdateAsync(int id, ResortCreateUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> AddUtilityToResortAsync(int resortId, AddUtilityToResortDTO dto);
        Task<bool> RemoveUtilityFromResortAsync(int resortId, int utilityId);
    }
}










