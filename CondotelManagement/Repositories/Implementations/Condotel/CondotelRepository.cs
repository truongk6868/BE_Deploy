using CondotelManagement.Data;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories
{
    public class CondotelRepository : ICondotelRepository
    {
        private readonly CondotelDbVer1Context _context;

        public CondotelRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }
        public void AddCondotel(Condotel condotel)
        {
            _context.Condotels.Add(condotel);
        }

    public void DeleteCondotel(int id)
    {
			var condotel = _context.Condotels
				   .Include(c => c.CondotelImages)
				   .Include(c => c.CondotelAmenities)
				   .Include(c => c.CondotelDetails)
				   .Include(c => c.CondotelPrices)
				   .Include(c => c.CondotelUtilities)
				   .Include(c => c.Promotions)
				   .Include(c => c.Vouchers)
				   .FirstOrDefault(c => c.CondotelId == id);

			if (condotel == null)
				return;

			// Soft delete Condotel chính
			condotel.Status = "Inactive";

			// Soft delete các bảng phụ
			foreach (var item in condotel.CondotelAmenities)
				item.Status = "Inactive";

			foreach (var item in condotel.CondotelDetails)
				item.Status = "Inactive";

			foreach (var item in condotel.CondotelPrices)
				item.Status = "Inactive";

			foreach (var item in condotel.CondotelUtilities)
				item.Status = "Inactive";

			foreach (var promo in condotel.Promotions)
				promo.Status = "Inactive";

			foreach (var voucher in condotel.Vouchers)
				voucher.Status = "Inactive";

			// Không gọi SaveChanges() ở đây, để Service quản lý transaction
		}

	public bool HasActiveBookings(int condotelId)
	{
		return _context.Bookings.Any(b => 
			b.CondotelId == condotelId && 
			(b.Status == "Pending" || b.Status == "Confirmed")
		);
	}

    public Condotel GetCondotelById(int id)
    {
        return _context.Condotels
            .Include(r => r.Resort)
            .Include(c => c.Host)
                .ThenInclude(h => h.User)
            .Include(c => c.CondotelImages)
            .Include(c => c.CondotelAmenities)
			.ThenInclude(ca => ca.Amenity)
			.Include(c => c.CondotelPrices)
            .Include(c => c.CondotelDetails)
            .Include(c => c.CondotelUtilities)
			.ThenInclude(cu => cu.Utility)
			.Include(c => c.Promotions)
			.Include(c => c.Reviews)
			.FirstOrDefault(c => c.CondotelId == id);
    }

        public IEnumerable<Condotel> GetCondtels()
        {
            return _context.Condotels
				.Where(c => c.Status == "Active")
				.Include(c => c.Resort)
                .Include(c => c.Host)
                .Include(c => c.CondotelImages)
                .Include(c => c.Promotions)
				.Include (c => c.CondotelPrices)
                .ToList();
        }

    public void UpdateCondotel(Condotel condotel)
    {
        // Lấy entity hiện có từ database
        var existing = _context.Condotels
            .Include(c => c.CondotelImages)
            .Include(c => c.CondotelAmenities)
            .Include(c => c.CondotelPrices)
            .Include(c => c.CondotelDetails)
            .Include(c => c.CondotelUtilities)
            .FirstOrDefault(c => c.CondotelId == condotel.CondotelId);

        if (existing == null)
            throw new InvalidOperationException($"Condotel with ID {condotel.CondotelId} not found");

        // Cập nhật các trường chính của Condotel
        existing.Name = condotel.Name;
        existing.Description = condotel.Description;
        existing.PricePerNight = condotel.PricePerNight;
        existing.Beds = condotel.Beds;
        existing.Bathrooms = condotel.Bathrooms;
        existing.Status = condotel.Status;
        existing.ResortId = condotel.ResortId;
        // HostId không được thay đổi khi update

        // Xóa các child entities cũ
        _context.CondotelImages.RemoveRange(existing.CondotelImages);
        _context.CondotelAmenities.RemoveRange(existing.CondotelAmenities);
        _context.CondotelPrices.RemoveRange(existing.CondotelPrices);
        _context.CondotelDetails.RemoveRange(existing.CondotelDetails);
        _context.CondotelUtilities.RemoveRange(existing.CondotelUtilities);

        // Thêm các child entities mới nếu có
        if (condotel.CondotelImages != null && condotel.CondotelImages.Any())
        {
            foreach (var image in condotel.CondotelImages)
            {
                image.CondotelId = existing.CondotelId;
                image.ImageId = 0; // Reset ID để tạo mới
            }
            _context.CondotelImages.AddRange(condotel.CondotelImages);
        }

        if (condotel.CondotelAmenities != null && condotel.CondotelAmenities.Any())
        {
            foreach (var amenity in condotel.CondotelAmenities)
            {
                amenity.CondotelId = existing.CondotelId;
            }
            _context.CondotelAmenities.AddRange(condotel.CondotelAmenities);
        }

        if (condotel.CondotelPrices != null && condotel.CondotelPrices.Any())
        {
            foreach (var price in condotel.CondotelPrices)
            {
                price.CondotelId = existing.CondotelId;
                price.PriceId = 0; // Reset ID để tạo mới
            }
            _context.CondotelPrices.AddRange(condotel.CondotelPrices);
        }

        if (condotel.CondotelDetails != null && condotel.CondotelDetails.Any())
        {
            foreach (var detail in condotel.CondotelDetails)
            {
                detail.CondotelId = existing.CondotelId;
                detail.DetailId = 0; // Reset ID để tạo mới
            }
            _context.CondotelDetails.AddRange(condotel.CondotelDetails);
        }

        if (condotel.CondotelUtilities != null && condotel.CondotelUtilities.Any())
        {
            foreach (var utility in condotel.CondotelUtilities)
            {
                utility.CondotelId = existing.CondotelId;
            }
            _context.CondotelUtilities.AddRange(condotel.CondotelUtilities);
        }

        // Mark entity as modified
        _context.Condotels.Update(existing);
    }
        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }

        public Promotion? GetPromotionById(int promotionId)
        {
            return _context.Promotions.FirstOrDefault(p => p.PromotionId == promotionId);
        }

        public IEnumerable<Condotel> GetCondtelsByHost(int hostId)
        {
            return _context.Condotels
                    .Where(c => c.HostId == hostId && c.Status == "Active")
                    .Include(c => c.Resort)
                    .Include(c => c.Host)
                    .Include(c => c.CondotelImages)
                    .Include(c => c.Promotions)
					.Include(c => c.CondotelPrices)
					.Include(c => c.Reviews)
					.ToList();
        }

	public IEnumerable<Condotel> GetCondotelsByFilters(
			string? name,
			string? location,
			int? locationId,
			DateOnly? fromDate,
			DateOnly? toDate,
			decimal? minPrice,
			decimal? maxPrice,
			int? beds,
			int? bathrooms)
	{
		// Validate date range trước
		if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
		{
			throw new ArgumentException("FromDate cannot be greater than ToDate");
		}

		// Bắt đầu với query cơ bản
		var query = _context.Condotels.AsQueryable().Where(c => c.Status == "Active");

		// Lọc theo tên condotel
		if (!string.IsNullOrWhiteSpace(name))
		{
			query = query.Where(c => c.Name.Contains(name));
		}

		// Lọc theo locationId (ưu tiên hơn location string)
		if (locationId.HasValue)
		{
			query = query.Where(c => 
				c.ResortId != null &&
				_context.Resorts.Any(r => 
					r.ResortId == c.ResortId && 
					r.LocationId == locationId.Value));
		}
		// Lọc theo location string (nếu không có locationId)
		else if (!string.IsNullOrWhiteSpace(location))
		{
			query = query.Where(c => 
				c.ResortId != null &&
				_context.Resorts.Any(r => 
					r.ResortId == c.ResortId && 
					r.Location != null &&
					r.Location.Name.Contains(location)));
		}

		// Lọc theo khoảng ngày - chỉ lấy condotel không có booking trong khoảng thời gian đó
		if (fromDate.HasValue && toDate.HasValue)
		{
			var fromDateValue = fromDate.Value;
			var toDateValue = toDate.Value;

			// Lấy danh sách CondotelId đã bị booking (chỉ tính booking chưa bị hủy)
			var bookedCondotelIds = _context.Bookings
				.Where(b => 
					b.Status != "Cancelled" &&
					b.StartDate <= toDateValue &&
					b.EndDate >= fromDateValue)
				.Select(b => b.CondotelId)
				.Distinct()
				.ToList();

			// Loại bỏ các condotel đã bị booking
			if (bookedCondotelIds.Any())
			{
				query = query.Where(c => !bookedCondotelIds.Contains(c.CondotelId));
			}
		}

			// ------------------------------------
			// 3. Lọc theo PricePerNight
			// ------------------------------------
			if (minPrice.HasValue)
				query = query.Where(c => c.PricePerNight >= minPrice.Value);

			if (maxPrice.HasValue)
				query = query.Where(c => c.PricePerNight <= maxPrice.Value);

			// ------------------------------------
			// 4. Lọc theo Beds & Bathrooms (filter trực tiếp từ Condotel)
			// ------------------------------------
			if (beds.HasValue)
			{
				query = query.Where(c => c.Beds >= beds.Value);
			}

			if (bathrooms.HasValue)
			{
				query = query.Where(c => c.Bathrooms >= bathrooms.Value);
			}

		// Include các navigation properties sau khi đã filter
		query = query
		.Include(c => c.Resort)
			.ThenInclude(r => r.Location)
		.Include(c => c.Host)
		.Include(c => c.CondotelImages)
		.Include(c => c.Promotions)
		.Include(c => c.CondotelPrices)
		.Include(c => c.Reviews);

	return query.ToList();
	}

	public bool ResortExists(int? resortId)
	{
		if (!resortId.HasValue) return true; // ResortId là optional
		return _context.Resorts.Any(r => r.ResortId == resortId.Value);
	}

	public bool AmenitiesExist(List<int>? amenityIds)
	{
		if (amenityIds == null || !amenityIds.Any()) return true; // Optional
		var existingCount = _context.Amenities.Count(a => amenityIds.Contains(a.AmenityId));
		return existingCount == amenityIds.Count;
	}

	public bool UtilitiesExist(List<int>? utilityIds)
	{
		if (utilityIds == null || !utilityIds.Any()) return true; // Optional
		var existingCount = _context.Utilities.Count(u => utilityIds.Contains(u.UtilityId));
		return existingCount == utilityIds.Count;
	}

	public bool HostExists(int hostId)
	{
		return _context.Hosts.Any(h => h.HostId == hostId);
	}

	public void AddCondotelImages(IEnumerable<CondotelImage> images)
	{
		if (images != null && images.Any())
		{
			_context.CondotelImages.AddRange(images);
		}
	}

	public void AddCondotelPrices(IEnumerable<CondotelPrice> prices)
	{
		if (prices != null && prices.Any())
		{
			_context.CondotelPrices.AddRange(prices);
		}
	}

	public void AddCondotelDetails(IEnumerable<CondotelDetail> details)
	{
		if (details != null && details.Any())
		{
			_context.CondotelDetails.AddRange(details);
		}
	}

	public void AddCondotelAmenities(IEnumerable<CondotelAmenity> amenities)
	{
		if (amenities != null && amenities.Any())
		{
			_context.CondotelAmenities.AddRange(amenities);
		}
	}

	public void AddCondotelUtilities(IEnumerable<CondotelUtility> utilities)
	{
		if (utilities != null && utilities.Any())
		{
			_context.CondotelUtilities.AddRange(utilities);
		}
	}

	public (int TotalCount, IEnumerable<Condotel> Items) GetCondtelsByHostPagedWithStatus(int hostId, string status, int pageNumber, int pageSize)
	{
		var query = _context.Condotels
			.Where(c => c.HostId == hostId && c.Status == status)
			.Include(c => c.Resort)
			.Include(c => c.Host)
				.ThenInclude(h => h.User)
			.Include(c => c.CondotelImages)
			.Include(c => c.Promotions)
			.Include(c => c.CondotelPrices)
			.Include(c => c.Reviews);

		var totalCount = query.Count();
		var items = query
			.OrderByDescending(c => c.CondotelId) // Sắp xếp mới nhất trước
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		return (totalCount, items);
	}

	public (int TotalCount, IEnumerable<Condotel> Items) GetCondtelsByHostPaged(int hostId, int pageNumber, int pageSize)
	{
		var query = _context.Condotels
			.Where(c => c.HostId == hostId && c.Status == "Active")
			.Include(c => c.Resort)
			.Include(c => c.Host)
			.Include(c => c.CondotelImages)
			.Include(c => c.Promotions)
			.Include(c => c.CondotelPrices)
			.Include(c => c.Reviews);

		var totalCount = query.Count();
		var items = query
			.OrderByDescending(c => c.CondotelId)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		return (totalCount, items);
	}

	public (int TotalCount, IEnumerable<Condotel> Items) GetCondotelsByFiltersPaged(
		string? name,
		string? location,
		int? locationId,
		DateOnly? fromDate,
		DateOnly? toDate,
		decimal? minPrice,
		decimal? maxPrice,
		int? beds,
		int? bathrooms,
		int pageNumber,
		int pageSize)
	{
		// Validate date range trước
		if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
		{
			throw new ArgumentException("FromDate cannot be greater than ToDate");
		}

		// Bắt đầu với query cơ bản
		var query = _context.Condotels.AsQueryable().Where(c => c.Status == "Active");

		// Lọc theo tên condotel
		if (!string.IsNullOrWhiteSpace(name))
		{
			query = query.Where(c => c.Name.Contains(name));
		}

		// Lọc theo locationId (ưu tiên hơn location string)
		if (locationId.HasValue)
		{
			query = query.Where(c => 
				c.ResortId != null &&
				_context.Resorts.Any(r => 
					r.ResortId == c.ResortId && 
					r.LocationId == locationId.Value));
		}
		// Lọc theo location string (nếu không có locationId)
		else if (!string.IsNullOrWhiteSpace(location))
		{
			query = query.Where(c => 
				c.ResortId != null &&
				_context.Resorts.Any(r => 
					r.ResortId == c.ResortId && 
					r.Location != null &&
					r.Location.Name.Contains(location)));
		}

		// Lọc theo khoảng ngày - chỉ lấy condotel không có booking trong khoảng thời gian đó
		if (fromDate.HasValue && toDate.HasValue)
		{
			var fromDateValue = fromDate.Value;
			var toDateValue = toDate.Value;

			// Lấy danh sách CondotelId đã bị booking (chỉ tính booking chưa bị hủy)
			var bookedCondotelIds = _context.Bookings
				.Where(b => 
					b.Status != "Cancelled" &&
					b.StartDate <= toDateValue &&
					b.EndDate >= fromDateValue)
				.Select(b => b.CondotelId)
				.Distinct()
				.ToList();

			// Loại bỏ các condotel đã bị booking
			if (bookedCondotelIds.Any())
			{
				query = query.Where(c => !bookedCondotelIds.Contains(c.CondotelId));
			}
		}

		// Lọc theo PricePerNight
		if (minPrice.HasValue)
			query = query.Where(c => c.PricePerNight >= minPrice.Value);

		if (maxPrice.HasValue)
			query = query.Where(c => c.PricePerNight <= maxPrice.Value);

		// Lọc theo Beds & Bathrooms
		if (beds.HasValue)
		{
			query = query.Where(c => c.Beds >= beds.Value);
		}

		if (bathrooms.HasValue)
		{
			query = query.Where(c => c.Bathrooms >= bathrooms.Value);
		}

		// Include các navigation properties
		query = query
			.Include(c => c.Resort)
				.ThenInclude(r => r.Location)
			.Include(c => c.Host)
			.Include(c => c.CondotelImages)
			.Include(c => c.Promotions)
			.Include(c => c.CondotelPrices)
			.Include(c => c.Reviews);

		var totalCount = query.Count();
		var items = query
			.OrderByDescending(c => c.CondotelId)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		return (totalCount, items);
	}

	public void UpdateCondotelStatus(int condotelId, string status)
	{
		var condotel = _context.Condotels.FirstOrDefault(c => c.CondotelId == condotelId);
		if (condotel != null)
		{
			condotel.Status = status;
		}
	}
	}
}
