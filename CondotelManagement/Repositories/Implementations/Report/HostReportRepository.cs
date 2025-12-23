using CondotelManagement.Data;
using CondotelManagement.DTOs;
using Microsoft.EntityFrameworkCore;
using System;

namespace CondotelManagement.Repositories 
{
    public class HostReportRepository : IHostReportRepository
    {
        private readonly CondotelDbVer1Context _db;

        public HostReportRepository(CondotelDbVer1Context db)
        {
            _db = db;
        }

        // Helper method để check status "Completed" (hỗ trợ cả tiếng Anh và tiếng Việt)
        private bool IsCompletedStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var trimmedStatus = status.Trim();
            return trimmedStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                   trimmedStatus.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase);
        }

        // Helper method để check status "Cancelled" (hỗ trợ cả tiếng Anh và tiếng Việt)
        private bool IsCancelledStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var trimmedStatus = status.Trim();
            return trimmedStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                   trimmedStatus.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase);
        }
        public async Task<HostReportDTO> GetHostReportAsync(int hostId, DateOnly? from, DateOnly? to)
        {
            var condotelIds = await _db.Condotels
            .Where(c => c.HostId == hostId)
            .Select(c => c.CondotelId)
            .ToListAsync();

            var bookings = _db.Bookings
                .Where(b => condotelIds.Contains(b.CondotelId));

            // Nếu truyền from/to → lọc
            if (from.HasValue)
            {
                bookings = bookings.Where(b => b.EndDate >= from.Value);
            }

            if (to.HasValue)
            {
                bookings = bookings.Where(b => b.StartDate <= to.Value);
            }

			int totalRooms = await _db.CondotelDetails
	            .CountAsync(d => condotelIds.Contains(d.CondotelId));

			int totalBookings = await bookings.CountAsync();

            int totalCancellations = await bookings
                .Where(b => b.Status == "Cancelled")
                .CountAsync();

			// ======== TÍNH SỐ PHÒNG ĐANG ĐƯỢC ĐẶT ========
			// Vì mỗi booking condotel = đặt tất cả phòng trong condotel đó
			int roomsBooked = await bookings
				.Where(b => b.Status == "Confirmed")
				.Join(_db.CondotelDetails,
					  b => b.CondotelId,
					  d => d.CondotelId,
					  (b, d) => d.DetailId)
				.Distinct()
				.CountAsync();

			// ======== TÍNH DOANH THU ========
			decimal revenue = await bookings
				.Where(b => b.Status == "Completed")
				.SumAsync(b => (decimal?)b.TotalPrice ?? 0m);

			// ======== TÍNH OCCUPANCY RATE ========
			var minDate = await _db.Bookings.MinAsync(b => b.StartDate);
			var maxDate = await _db.Bookings.MaxAsync(b => b.EndDate);
			var fromDate = from ?? minDate;
			var toDate = to ?? maxDate;

			var days = (toDate.ToDateTime(TimeOnly.MinValue) - fromDate.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;

			// bookedNights = tổng số đêm * số phòng condotel được đặt
			var bookedNights = await bookings
				.Where(b => b.Status != "Cancelled")
				.Join(_db.CondotelDetails,
					  b => b.CondotelId,
					  d => d.CondotelId,
					  (b, d) => new { b, d })
				.SumAsync(x =>
					(double)EF.Functions.DateDiffDay(
						x.b.StartDate < from ? from : x.b.StartDate,
						x.b.EndDate > to ? to : x.b.EndDate
					)
				);

			double possibleNights = totalRooms * days;
			//Occupancy = (Booked Nights / (Total Rooms * Number of Days)) * 100
			//Occupancy rate = số đêm phòng được đặt / số đêm phòng có thể bán trong khoảng thời gian đó
			double occupancyRate = possibleNights > 0 ? (bookedNights / possibleNights) * 100 : 0;

			int completedBookings = await bookings
				.Where(b => b.Status == "Completed")
				.CountAsync();

			// ======== TÍNH TỔNG KHÁCH HÀNG (UNIQUE) ========
			int totalCustomers = await bookings
				.Select(b => b.CustomerId)
				.Distinct()
				.CountAsync();

			// ======== TÍNH GIÁ TRỊ TRUNG BÌNH MỖI ĐẶT PHÒNG ========
			// Tính từ các booking đã hoàn thành
			decimal averageBookingValue = completedBookings > 0 
				? Math.Round(revenue / completedBookings, 2) 
				: 0m;

			// ======== TÍNH SỐ BOOKING ĐANG XỬ LÝ ========
			int pendingBookings = await bookings
				.Where(b => b.Status == "Pending")
				.CountAsync();

			// ======== TÍNH SỐ BOOKING ĐÃ XÁC NHẬN ========
			int confirmedBookings = await bookings
				.Where(b => b.Status == "Confirmed")
				.CountAsync();

			return new HostReportDTO
			{
				Revenue = Math.Round(revenue, 2),
				TotalRooms = totalRooms,
				RoomsBooked = roomsBooked,
				OccupancyRate = Math.Round(occupancyRate, 2),
				TotalBookings = totalBookings,
				TotalCancellations = totalCancellations,
				CompletedBookings = completedBookings,
				TotalCustomers = totalCustomers,
				AverageBookingValue = averageBookingValue,
				PendingBookings = pendingBookings,
				ConfirmedBookings = confirmedBookings
			};
        }

		public async Task<RevenueReportResponseDTO> GetRevenueReportByMonthYearAsync(int hostId, int? year, int? month)
		{
			// Lấy danh sách CondotelId của host
			var condotelIds = await _db.Condotels
				.Where(c => c.HostId == hostId)
				.Select(c => c.CondotelId)
				.ToListAsync();

			// Debug: Log condotelIds
			Console.WriteLine($"[RevenueReport] ========== START ==========");
			Console.WriteLine($"[RevenueReport] HostId: {hostId}, CondotelIds: [{string.Join(", ", condotelIds)}]");

			// Kiểm tra nếu host không có condotel nào
			if (!condotelIds.Any())
			{
				Console.WriteLine($"[RevenueReport] Host {hostId} không có condotel nào");
				return new RevenueReportResponseDTO
				{
					TotalRevenue = 0,
					TotalBookings = 0,
					CompletedBookings = 0,
					CancelledBookings = 0,
					MonthlyRevenues = new List<MonthlyRevenueDTO>(),
					YearlyRevenues = new List<YearlyRevenueDTO>()
				};
			}

			// Lấy TẤT CẢ bookings của host (KHÔNG FILTER STATUS) để debug
			var allBookingsRaw = await _db.Bookings
				.Where(b => condotelIds.Contains(b.CondotelId))
				.ToListAsync();

			Console.WriteLine($"[RevenueReport] TẤT CẢ bookings của host (không filter): {allBookingsRaw.Count}");
			foreach (var b in allBookingsRaw)
			{
				Console.WriteLine($"  - BookingId: {b.BookingId}, CondotelId: {b.CondotelId}, Status: '{b.Status}', TotalPrice: {b.TotalPrice}, EndDate: {b.EndDate}");
			}

			// Lấy tất cả bookings của host (bao gồm tất cả status để tính tổng số booking)
			var allBookingsQuery = _db.Bookings
				.Where(b => condotelIds.Contains(b.CondotelId))
				.AsQueryable();

			// Lấy chỉ bookings Completed để tính doanh thu (CHỈ TÍNH DOANH THU TỪ COMPLETED)
			// Lấy tất cả booking trước, sau đó filter trong memory để tránh vấn đề case-sensitive
			var completedBookingsQuery = _db.Bookings
				.Where(b => condotelIds.Contains(b.CondotelId))
				.AsQueryable();

			// Filter theo năm nếu có (filter theo năm của EndDate)
			if (year.HasValue)
			{
				allBookingsQuery = allBookingsQuery.Where(b => b.EndDate.Year == year.Value);
				completedBookingsQuery = completedBookingsQuery.Where(b => b.EndDate.Year == year.Value);
				Console.WriteLine($"[RevenueReport] Filter theo năm: {year.Value}");
			}

			// Filter theo tháng nếu có (chỉ filter khi đã có year)
			if (month.HasValue)
			{
				allBookingsQuery = allBookingsQuery.Where(b => b.EndDate.Month == month.Value);
				completedBookingsQuery = completedBookingsQuery.Where(b => b.EndDate.Month == month.Value);
				Console.WriteLine($"[RevenueReport] Filter theo tháng: {month.Value}");
			}

			var allBookings = await allBookingsQuery.ToListAsync();
			var completedBookingsRaw = await completedBookingsQuery.ToListAsync();
			
			// Filter Completed trong memory (case-insensitive)
			// Hỗ trợ cả "Completed" (tiếng Anh) và "Hoàn thành" (tiếng Việt)
			// Year/month filter đã được apply trong query rồi
			var completedBookings = completedBookingsRaw
				.Where(b => IsCompletedStatus(b.Status))
				.ToList();

			// Debug: Log số lượng booking
			Console.WriteLine($"[RevenueReport] AllBookings (sau filter): {allBookings.Count}, CompletedBookings: {completedBookings.Count}");
			
			// Log tất cả status để debug
			var statusGroups = allBookingsRaw.GroupBy(b => b.Status).ToList();
			Console.WriteLine($"[RevenueReport] Phân bố Status:");
			foreach (var g in statusGroups)
			{
				Console.WriteLine($"  - Status '{g.Key}': {g.Count()} bookings");
			}

			if (completedBookings.Any())
			{
				Console.WriteLine($"[RevenueReport] Completed bookings details (sau filter):");
				foreach (var b in completedBookings)
				{
					Console.WriteLine($"  - BookingId: {b.BookingId}, TotalPrice: {b.TotalPrice}, Status: '{b.Status}', EndDate: {b.EndDate}");
				}
			}
			else
			{
				Console.WriteLine($"[RevenueReport] ⚠️ KHÔNG TÌM THẤY BOOKING COMPLETED!");
				// Kiểm tra xem có booking nào với status khác "Completed" không
				var otherStatusBookings = allBookingsRaw.Where(b => b.Status.ToLower().Contains("complete")).ToList();
				if (otherStatusBookings.Any())
				{
					Console.WriteLine($"[RevenueReport] Tìm thấy {otherStatusBookings.Count} booking có chứa 'complete' trong status:");
					foreach (var b in otherStatusBookings)
					{
						Console.WriteLine($"  - BookingId: {b.BookingId}, Status: '{b.Status}' (exact: '{b.Status}')");
					}
				}
			}

			// Tính tổng doanh thu (CHỈ TỪ BOOKING COMPLETED)
			// Tính tất cả booking Completed, nếu TotalPrice null thì coi như 0
			var totalRevenue = completedBookings
				.Sum(b => b.TotalPrice ?? 0m);
			
			// Debug: Log doanh thu
			Console.WriteLine($"[RevenueReport] TotalRevenue calculated: {totalRevenue}");
			Console.WriteLine($"[RevenueReport] ========== END ==========");

			// Tính tổng số booking (tất cả status)
			var totalBookings = allBookings.Count;

			// Tính số booking Completed
			var completedBookingsCount = completedBookings.Count;

			// Tính số booking Cancelled (hỗ trợ cả tiếng Anh và tiếng Việt)
			var cancelledBookings = allBookings.Count(b => IsCancelledStatus(b.Status));

			// Nhóm theo tháng/năm (dùng allBookings để có tổng số booking, nhưng chỉ tính revenue từ completed)
			var monthlyData = allBookings
				.GroupBy(b => new { b.EndDate.Year, b.EndDate.Month })
				.Select(g => new MonthlyRevenueDTO
				{
					Year = g.Key.Year,
					Month = g.Key.Month,
					MonthName = $"Tháng {g.Key.Month}",
					// CHỈ TÍNH REVENUE TỪ BOOKING COMPLETED (case-insensitive, hỗ trợ cả tiếng Anh và tiếng Việt)
					Revenue = g.Where(b => IsCompletedStatus(b.Status))
						.Sum(b => b.TotalPrice ?? 0m),
					TotalBookings = g.Count(),
					CompletedBookings = g.Count(b => IsCompletedStatus(b.Status)),
					CancelledBookings = g.Count(b => IsCancelledStatus(b.Status))
				})
				.OrderBy(m => m.Year)
				.ThenBy(m => m.Month)
				.ToList();

			// Nhóm theo năm
			var yearlyData = allBookings
				.GroupBy(b => b.EndDate.Year)
				.Select(g => new YearlyRevenueDTO
				{
					Year = g.Key,
					// CHỈ TÍNH REVENUE TỪ BOOKING COMPLETED (case-insensitive, hỗ trợ cả tiếng Anh và tiếng Việt)
					TotalRevenue = g.Where(b => IsCompletedStatus(b.Status))
						.Sum(b => b.TotalPrice ?? 0m),
					TotalBookings = g.Count(),
					CompletedBookings = g.Count(b => IsCompletedStatus(b.Status)),
					CancelledBookings = g.Count(b => IsCancelledStatus(b.Status)),
					MonthlyData = g
						.GroupBy(b => b.EndDate.Month)
						.Select(mg => new MonthlyRevenueDTO
						{
							Year = g.Key,
							Month = mg.Key,
							MonthName = $"Tháng {mg.Key}",
							// CHỈ TÍNH REVENUE TỪ BOOKING COMPLETED (case-insensitive, hỗ trợ cả tiếng Anh và tiếng Việt)
							Revenue = mg.Where(b => IsCompletedStatus(b.Status))
								.Sum(b => b.TotalPrice ?? 0m),
							TotalBookings = mg.Count(),
							CompletedBookings = mg.Count(b => IsCompletedStatus(b.Status)),
							CancelledBookings = mg.Count(b => IsCancelledStatus(b.Status))
						})
						.OrderBy(m => m.Month)
						.ToList()
				})
				.OrderBy(y => y.Year)
				.ToList();

			// Debug: Log monthlyData và yearlyData trước khi return
			Console.WriteLine($"[RevenueReport] MonthlyData count: {monthlyData.Count}");
			foreach (var m in monthlyData)
			{
				Console.WriteLine($"  - Month: {m.Month}/{m.Year}, Revenue: {m.Revenue}, TotalBookings: {m.TotalBookings}, CompletedBookings: {m.CompletedBookings}");
			}
			
			Console.WriteLine($"[RevenueReport] YearlyData count: {yearlyData.Count}");
			foreach (var y in yearlyData)
			{
				Console.WriteLine($"  - Year: {y.Year}, TotalRevenue: {y.TotalRevenue}, TotalBookings: {y.TotalBookings}, CompletedBookings: {y.CompletedBookings}, MonthlyData count: {y.MonthlyData.Count}");
			}

			var response = new RevenueReportResponseDTO
			{
				TotalRevenue = Math.Round(totalRevenue, 2),
				TotalBookings = totalBookings,
				CompletedBookings = completedBookingsCount,
				CancelledBookings = cancelledBookings,
				MonthlyRevenues = monthlyData,
				YearlyRevenues = yearlyData
			};

			// Debug: Log response structure
			Console.WriteLine($"[RevenueReport] Response - TotalRevenue: {response.TotalRevenue}, TotalBookings: {response.TotalBookings}");
			Console.WriteLine($"[RevenueReport] Response - MonthlyRevenues.Count: {response.MonthlyRevenues?.Count ?? 0}");
			Console.WriteLine($"[RevenueReport] Response - YearlyRevenues.Count: {response.YearlyRevenues?.Count ?? 0}");

			return response;
		}
    }
}
