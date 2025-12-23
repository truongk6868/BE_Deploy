using CondotelManagement.Data;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CondotelManagement.Repositories
{
	public class VoucherRepository : IVoucherRepository
	{
		private readonly CondotelDbVer1Context _context;

		public VoucherRepository(CondotelDbVer1Context context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Voucher>> GetByHostAsync(int hostId)
		{
			return await _context.Vouchers
				.Include(v => v.Condotel)
				.Include(v => v.User)
				.Where(v => v.Condotel != null && v.Condotel.HostId == hostId && v.Status == "Active")
				.ToListAsync();
		}

		public async Task<IEnumerable<Voucher>> GetByCondotelAsync(int condotelId)
		{
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			return await _context.Vouchers
				.Include(v => v.Condotel)
				.Include(v => v.User)
				.Where(v => v.CondotelId == condotelId 
					&& v.Status == "Active"
					&& v.EndDate >= today) // Chỉ lấy voucher còn hiệu lực
				.OrderByDescending(v => v.EndDate) // Sắp xếp theo ngày hết hạn
				.ToListAsync();
		}

		public async Task<IEnumerable<Voucher>> GetByUserIdAsync(int userId)
		{
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			return await _context.Vouchers
				.Include(v => v.Condotel)
				.Include(v => v.User)
				.Where(v => v.UserId == userId 
					&& v.Status == "Active"
					&& v.EndDate >= today) // Chỉ lấy voucher còn hiệu lực
				.OrderByDescending(v => v.EndDate) // Sắp xếp theo ngày hết hạn (gần nhất trước)
				.ToListAsync();
		}

		public async Task<Voucher?> GetByIdAsync(int id)
		{
			return await _context.Vouchers.FindAsync(id);
		}

		public async Task<Voucher?> GetByCodeAsync(string code)
		{
			return await _context.Vouchers
				.Include(v => v.Condotel)
				.Include(v => v.User)
				.FirstOrDefaultAsync(v => v.Code == code);
		}

		public async Task<Voucher> AddAsync(Voucher voucher)
		{
			_context.Vouchers.Add(voucher);
			await _context.SaveChangesAsync();
			// Load lại để có Condotel & User
			var saved = await _context.Vouchers
				.Include(v => v.Condotel)
				.Include(v => v.User)
				.FirstOrDefaultAsync(v => v.VoucherId == voucher.VoucherId);
			return saved;
		}

		public async Task<Voucher?> UpdateAsync(Voucher voucher)
		{
			var existing = await _context.Vouchers.FindAsync(voucher.VoucherId);
			if (existing == null) return null;

			_context.Entry(existing).CurrentValues.SetValues(voucher);
			await _context.SaveChangesAsync();
			// Load lại để có Condotel & User
			var saved = await _context.Vouchers
				.Include(v => v.Condotel)
				.Include(v => v.User)
				.FirstOrDefaultAsync(v => v.VoucherId == voucher.VoucherId);
			return saved;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			var existing = await _context.Vouchers.FindAsync(id);
			if (existing == null) return false;

			// Soft delete: chuyển trạng thái
			existing.Status = "Inactive";
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<string> GenerateUniqueVoucherCodeAsync(int userId, int maxRetries = 5)
		{
			for (int i = 0; i < maxRetries; i++)
			{
				// Tạo code dạng: BOOK + UserID + 6 ký tự random (chữ + số)
				var randomPart = Path.GetRandomFileName().Replace(".", "").Substring(0, 6).ToUpper();
				string code = $"BOOK{userId}{randomPart}";

				// Kiểm tra đã tồn tại trong DB chưa
				bool exists = await _context.Vouchers.AnyAsync(v => v.Code == code);
				if (!exists)
					return code; // unique -> trả về

				// nếu trùng -> retry
			}

			throw new Exception("Cannot generate unique voucher code after multiple attempts");
		}

		public async Task<HostVoucherSetting?> GetByHostIdAsync(int hostId)
		{
			return await _context.HostVoucherSettings
				.FirstOrDefaultAsync(x => x.HostID == hostId);
		}

		public async Task<HostVoucherSetting> AddOrUpdateAsync(HostVoucherSetting setting)
		{
			var exist = await _context.HostVoucherSettings
				.FirstOrDefaultAsync(x => x.HostID == setting.HostID);

			if (exist == null)
			{
				_context.HostVoucherSettings.Add(setting);
				await _context.SaveChangesAsync();
				return setting; // SettingID sẽ được gán
			}
			else
			{
				exist.DiscountAmount = setting.DiscountAmount;
				exist.DiscountPercentage = setting.DiscountPercentage;
				exist.AutoGenerate = setting.AutoGenerate;
				exist.ValidMonths = setting.ValidMonths;
				exist.UsageLimit = setting.UsageLimit;
				await _context.SaveChangesAsync();
				return exist; // ← Trả về entity tồn tại để có SettingID
			}
		}
	}
}
