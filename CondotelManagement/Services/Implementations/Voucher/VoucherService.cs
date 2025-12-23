using CondotelManagement.DTOs;
using CondotelManagement.Repositories;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services
{
	public class VoucherService : IVoucherService
	{
		private readonly IVoucherRepository _repo;
		private readonly ICondotelRepository _condotelRepo;
		private readonly IBookingRepository _bookingRepo;

		public VoucherService(IVoucherRepository repo, ICondotelRepository condotelRepository, IBookingRepository bookingRepo)
		{
			_repo = repo;
			_condotelRepo = condotelRepository;
			_bookingRepo = bookingRepo;
		}

		public async Task<IEnumerable<VoucherDTO>> GetVouchersByHostAsync(int hostId)
		{
			var list = await _repo.GetByHostAsync(hostId);
			return list.Select(v => new VoucherDTO
			{
				VoucherID = v.VoucherId,
				CondotelID = v.Condotel?.CondotelId,
				CondotelName = v.Condotel?.Name,
				UserID = v.User?.UserId,
				FullName = v.User?.FullName,
				Code = v.Code,
				DiscountAmount = v.DiscountAmount,
				DiscountPercentage = v.DiscountPercentage,
				StartDate = v.StartDate,
				EndDate = v.EndDate,
				Status = v.Status
			});
		}

		public async Task<IEnumerable<VoucherDTO>> GetVouchersByCondotelAsync(int condotelId)
		{
			var list = await _repo.GetByCondotelAsync(condotelId);
			return list.Select(v => new VoucherDTO
			{
				VoucherID = v.VoucherId,
				CondotelID = v.Condotel?.CondotelId,
				CondotelName = v.Condotel?.Name,
				UserID = v.User?.UserId,
				FullName = v.User?.FullName,
				Code = v.Code,
				DiscountAmount = v.DiscountAmount,
				DiscountPercentage = v.DiscountPercentage,
				StartDate = v.StartDate,
				EndDate = v.EndDate,
				Status = v.Status
			});
		}

		public async Task<IEnumerable<VoucherDTO>> GetVouchersByUserIdAsync(int userId)
		{
			var list = await _repo.GetByUserIdAsync(userId);
			return list.Select(v => new VoucherDTO
			{
				VoucherID = v.VoucherId,
				CondotelID = v.Condotel?.CondotelId,
				CondotelName = v.Condotel?.Name,
				UserID = v.User?.UserId,
				FullName = v.User?.FullName,
				Code = v.Code,
				DiscountAmount = v.DiscountAmount,
				DiscountPercentage = v.DiscountPercentage,
				StartDate = v.StartDate,
				EndDate = v.EndDate,
				Status = v.Status
			});
		}

		public async Task<VoucherDTO?> CreateVoucherAsync(VoucherCreateDTO dto)
		{
			// Validate: Voucher PHẢI có CondotelID (voucher chỉ áp dụng cho condotel cụ thể)
			if (!dto.CondotelID.HasValue || dto.CondotelID.Value <= 0)
			{
				throw new ArgumentException("CondotelID is required. Voucher must be associated with a specific condotel.");
			}

			var entity = new Voucher
			{
				CondotelId = dto.CondotelID.Value, // Bắt buộc có giá trị
				UserId = dto.UserID,
				Code = dto.Code,
				DiscountAmount = dto.DiscountAmount,
				DiscountPercentage = dto.DiscountPercentage,
				StartDate = dto.StartDate,
				EndDate = dto.EndDate,
				UsageLimit = dto.UsageLimit,
				Status = "Active"
			};
			var saved = await _repo.AddAsync(entity);

			return new VoucherDTO
			{
				VoucherID = saved.VoucherId,
				CondotelID = saved.Condotel.CondotelId,
				CondotelName = saved.Condotel.Name,
				UserID = saved.User?.UserId,
				FullName = saved.User?.FullName,
				Code = saved.Code,
				DiscountAmount = saved.DiscountAmount,
				DiscountPercentage = saved.DiscountPercentage,
				StartDate = saved.StartDate,
				EndDate = saved.EndDate,
				Status = saved.Status
			};
		}

		public async Task<VoucherDTO?> UpdateVoucherAsync(int id, VoucherCreateDTO dto)
		{
			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return null;

			existing.CondotelId = dto.CondotelID;
			existing.UserId = dto.UserID;
			existing.Code = dto.Code;
			existing.DiscountAmount = dto.DiscountAmount;
			existing.DiscountPercentage = dto.DiscountPercentage;
			existing.StartDate = dto.StartDate;
			existing.EndDate = dto.EndDate;
			existing.UsageLimit = dto.UsageLimit;

			var updated = await _repo.UpdateAsync(existing);
			if (updated == null) return null;

			return new VoucherDTO
			{
				VoucherID = updated.VoucherId,
				CondotelID = updated.Condotel.CondotelId,
				CondotelName = updated.Condotel.Name,
				UserID = updated.User?.UserId,
				FullName = updated.User?.FullName,
				Code = updated.Code,
				DiscountAmount = updated.DiscountAmount,
				DiscountPercentage = updated.DiscountPercentage,
				StartDate = updated.StartDate,
				EndDate = updated.EndDate,
				Status = updated.Status
			};
		}

		public Task<bool> DeleteVoucherAsync(int id) => _repo.DeleteAsync(id);

	public async Task<List<VoucherDTO>> CreateVoucherAfterBookingAsync(int bookingId)
	{
		// 1. Lấy booking
		var booking = _bookingRepo.GetBookingById(bookingId);
		if (booking == null)
			return new List<VoucherDTO>();

		// QUAN TRỌNG: Chỉ tạo voucher khi booking đã Completed
		// Voucher KHÔNG được tạo khi booking Confirmed (khi thanh toán thành công)
		if (booking.Status != "Completed")
		{
			Console.WriteLine($"[CreateVoucherAfterBookingAsync] Booking {bookingId} có status '{booking.Status}', không phải 'Completed'. Không tạo voucher.");
			return new List<VoucherDTO>();
		}

		int userId = booking.CustomerId;
		int condotelId = booking.CondotelId;

			// 2. Lấy condotel để biết HostID
			var condotel = _condotelRepo.GetCondotelById(condotelId);
			if (condotel == null)
				return new List<VoucherDTO>();

			int hostId = condotel.HostId;

			// 3. Lấy setting của Host
			var setting = await _repo.GetByHostIdAsync(hostId);
			if (setting == null || !setting.AutoGenerate)
				return new List<VoucherDTO>();

			// 4. Lấy tất cả condotel thuộc Host
			var condotels = _condotelRepo.GetCondtelsByHost(hostId);

			List<VoucherDTO> vouchers = new();

			foreach (var c in condotels)
			{
				// 4.1. Sinh mã code cho từng condotel
				string code = await _repo.GenerateUniqueVoucherCodeAsync(userId);

				var voucher = new Voucher
				{
					CondotelId = c.CondotelId,
					UserId = userId,
					Code = code,
					DiscountAmount = setting.DiscountAmount ?? 0,
					DiscountPercentage = setting.DiscountPercentage ?? 0,
					StartDate = DateOnly.FromDateTime(DateTime.Today),
					EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(setting.ValidMonths)),
					UsageLimit = setting.UsageLimit,
					Status = "Active"
				};

				// 4.2 Lưu vào DB
				var saved = await _repo.AddAsync(voucher);

				// 4.3 Map DTO
				vouchers.Add(new VoucherDTO
				{
					VoucherID = saved.VoucherId,
					CondotelID = c.CondotelId,
					CondotelName = c.Name,
					UserID = userId,
					FullName = saved.User.FullName,
					Code = saved.Code,
					DiscountAmount = saved.DiscountAmount,
					DiscountPercentage = saved.DiscountPercentage,
					StartDate = saved.StartDate,
					EndDate = saved.EndDate,
					Status = saved.Status
				});
			}

			return vouchers;
		}

		public async Task<HostVoucherSettingDetailDTO?> GetSettingAsync(int hostId)
		{
			var setting = await _repo.GetByHostIdAsync(hostId);
			if (setting == null) return null;

			return new HostVoucherSettingDetailDTO
			{
				SettingID = setting.SettingID,
				HostID = setting.HostID,
				DiscountAmount = setting.DiscountAmount,
				DiscountPercentage = setting.DiscountPercentage,
				AutoGenerate = setting.AutoGenerate,
				ValidMonths = setting.ValidMonths,
				UsageLimit = setting.UsageLimit
			};
		}

		public async Task<HostVoucherSettingDetailDTO> SaveSettingAsync(int hostId, HostVoucherSettingDTO dto)
		{
			var entity = new HostVoucherSetting
			{
				HostID = hostId,
				DiscountAmount = dto.DiscountAmount,
				DiscountPercentage = dto.DiscountPercentage,
				AutoGenerate = dto.AutoGenerate,
				UsageLimit = dto.UsageLimit,
				ValidMonths = dto.ValidMonths
			};

			var saved = await _repo.AddOrUpdateAsync(entity);

			return new HostVoucherSettingDetailDTO
			{
				SettingID = saved.SettingID,
				HostID = saved.HostID,
				DiscountAmount = saved.DiscountAmount,
				DiscountPercentage = saved.DiscountPercentage,
				AutoGenerate = saved.AutoGenerate,
				UsageLimit = saved.UsageLimit,
				ValidMonths = saved.ValidMonths
			};
		}

		public async Task<Voucher?> ValidateVoucherByCodeAsync(string code, int condotelId, int userId, DateOnly bookingDate)
		{
			var voucher = await _repo.GetByCodeAsync(code);
			if (voucher == null)
				return null;

			// Kiểm tra status
			if (voucher.Status != "Active")
				return null;

			// Kiểm tra thời hạn
			var today = DateOnly.FromDateTime(DateTime.UtcNow);
			if (bookingDate < voucher.StartDate || bookingDate > voucher.EndDate)
				return null;

			// Kiểm tra condotel - Voucher PHẢI có CondotelId và phải match với condotel đang booking
			// Voucher chỉ áp dụng cho những condotel nhất định (không cho phép voucher dùng cho tất cả condotel)
			if (!voucher.CondotelId.HasValue)
			{
				// Voucher không có CondotelId → không hợp lệ (voucher phải gắn với condotel cụ thể)
				return null;
			}

			if (voucher.CondotelId.Value != condotelId)
			{
				// Voucher không thuộc về condotel này
				return null;
			}

			// QUAN TRỌNG: Kiểm tra quyền sở hữu voucher
			// - Nếu voucher có UserId (voucher cá nhân): CHỈ user đó mới được dùng
			// - Nếu voucher không có UserId (voucher công khai): Bất kỳ ai cũng được dùng
			if (voucher.UserId.HasValue && voucher.UserId.Value != userId)
			{
				// Voucher thuộc về user khác → không được phép sử dụng
				return null;
			}

			// Kiểm tra usage limit
			if (voucher.UsageLimit.HasValue)
			{
				var usedCount = voucher.UsedCount ?? 0;
				if (usedCount >= voucher.UsageLimit.Value)
					return null;
			}

			return voucher;
		}

		public async Task ApplyVoucherToBookingAsync(int voucherId)
		{
			var voucher = await _repo.GetByIdAsync(voucherId);
			if (voucher == null) return;

			// Tăng UsedCount
			voucher.UsedCount = (voucher.UsedCount ?? 0) + 1;

			// Nếu đã đạt giới hạn, set status = "Used"
			if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
			{
				voucher.Status = "Used";
			}

			await _repo.UpdateAsync(voucher);
		}

		public async Task RollbackVoucherUsageAsync(int voucherId)
		{
			var voucher = await _repo.GetByIdAsync(voucherId);
			if (voucher == null) return;

			// Giảm UsedCount
			var currentUsedCount = voucher.UsedCount ?? 0;
			if (currentUsedCount > 0)
			{
				voucher.UsedCount = currentUsedCount - 1;

				// Nếu status là "Used" và UsedCount < UsageLimit, set lại "Active"
				if (voucher.Status == "Used" && voucher.UsageLimit.HasValue && voucher.UsedCount < voucher.UsageLimit.Value)
				{
					voucher.Status = "Active";
				}

				await _repo.UpdateAsync(voucher);
			}
		}
	}
}
