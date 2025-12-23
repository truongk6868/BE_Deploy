using CondotelManagement.Models;

namespace CondotelManagement.Repositories
{
    public interface IResortRepository
    {
        Task<IEnumerable<Resort>> GetAllAsync();
        Task<Resort?> GetByIdAsync(int id);
        Task<IEnumerable<Resort>> GetByLocationIdAsync(int locationId);
        Task<Resort> AddAsync(Resort resort);
        Task UpdateAsync(Resort resort);
        Task DeleteAsync(Resort resort);
    }
}










