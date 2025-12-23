using CondotelManagement.DTOs;
using CondotelManagement.Models;
using Google.Apis.Util;

namespace CondotelManagement.Repositories
{
	public interface IUtilitiesRepository
	{
		Task<IEnumerable<Utility>> GetAllAsync();
		Task<Utility?> GetByIdAsync(int id);
		Task<Utility> CreateAsync(Utility model);
		Task<bool> UpdateAsync(Utility model);
		Task<bool> DeleteAsync(int id);
		Task<IEnumerable<Utility>> GetByResortAsync(int resortId);
	}
}
