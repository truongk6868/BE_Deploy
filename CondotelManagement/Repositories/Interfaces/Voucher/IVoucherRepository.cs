using CondotelManagement.Models;

namespace CondotelManagement.Repositories
{
	public interface IVoucherRepository
	{
		Task<IEnumerable<Voucher>> GetByHostAsync(int hostId);
		Task<IEnumerable<Voucher>> GetByCondotelAsync(int condotelId);
		Task<IEnumerable<Voucher>> GetByUserIdAsync(int userId);
		Task<Voucher?> GetByIdAsync(int id);
		Task<Voucher?> GetByCodeAsync(string code);
		Task<Voucher> AddAsync(Voucher voucher);
		Task<Voucher?> UpdateAsync(Voucher voucher);
		Task<bool> DeleteAsync(int id);
		Task<string> GenerateUniqueVoucherCodeAsync(int userId, int maxRetries = 5);
		Task<HostVoucherSetting?> GetByHostIdAsync(int hostId);
		Task<HostVoucherSetting> AddOrUpdateAsync(HostVoucherSetting setting);
	}
}
