using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using CondotelManagement.Data;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Controllers
{
	[ApiController]
	[Route("api/tenant/condotels")]
	public class CondotelController : ControllerBase
	{
		private readonly ICondotelService _condotelService;
		private readonly IServicePackageService _servicePackageService;
		private readonly CondotelDbVer1Context _context;

		public CondotelController(ICondotelService condotelService, IServicePackageService servicePackageService, CondotelDbVer1Context context)
		{
			_condotelService = condotelService;
			_servicePackageService = servicePackageService;
			_context = context;
		}

		// GET api/tenant/condotels?name=abc&location=abc&locationId=1&fromDate=...&toDate=...&pageNumber=1&pageSize=10
		[HttpGet]
		[AllowAnonymous]
		public ActionResult<object> GetCondotelsByFilters(
				[FromQuery] string? name, 
				[FromQuery] string? location,
				[FromQuery] int? locationId,
				[FromQuery] DateOnly? fromDate, 
				[FromQuery] DateOnly? toDate,
				[FromQuery] decimal? minPrice,
				[FromQuery] decimal? maxPrice,
				[FromQuery] int? beds,
				[FromQuery] int? bathrooms,
				[FromQuery] int pageNumber = 1,
				[FromQuery] int pageSize = 10)
		{
			// Validate pagination parameters
			if (pageNumber < 1)
				pageNumber = 1;
			if (pageSize < 1 || pageSize > 100)
				pageSize = 10; // Default 10, max 100

			var result = _condotelService.GetCondotelsByFiltersPaged(
				name, location, locationId, fromDate, toDate, minPrice, maxPrice, beds, bathrooms, pageNumber, pageSize);
			
			return Ok(new
			{
				success = true,
				data = result.Items,
				pagination = new
				{
					pageNumber = result.PageNumber,
					pageSize = result.PageSize,
					totalCount = result.TotalCount,
					totalPages = result.TotalPages,
					hasPreviousPage = result.HasPreviousPage,
					hasNextPage = result.HasNextPage
				}
			});
		}

		// GET api/tenant/condotels/{id}/amenities-utilities - Lấy cả amenities và utilities (PHẢI ĐẶT TRƯỚC {id})
		[HttpGet("{id}/amenities-utilities")]
		[AllowAnonymous]
		public async Task<ActionResult<object>> GetCondotelAmenitiesAndUtilities(int id)
		{
			// Kiểm tra condotel có tồn tại không
			var condotelExists = await _context.Condotels.AnyAsync(c => c.CondotelId == id);
			if (!condotelExists)
				return NotFound(new { message = "Không tìm thấy condotel" });

			// Query trực tiếp từ database
			// Lấy tất cả amenities (bỏ filter Status để debug - có thể Status khác "Active")
			var amenities = await _context.CondotelAmenities
				.Include(ca => ca.Amenity)
				.Where(ca => ca.CondotelId == id && ca.Amenity != null)
				.Select(ca => new AmenityDTO
				{
					AmenityId = ca.Amenity.AmenityId,
					Name = ca.Amenity.Name
				})
				.ToListAsync();

			// Lấy tất cả utilities (bỏ filter Status để debug - có thể Status khác "Active")
			var utilities = await _context.CondotelUtilities
				.Include(cu => cu.Utility)
				.Where(cu => cu.CondotelId == id && cu.Utility != null)
				.Select(cu => new UtilityDTO
				{
					UtilityId = cu.Utility.UtilityId,
					Name = cu.Utility.Name
				})
				.ToListAsync();

			return Ok(new
			{
				condotelId = id,
				amenities,
				utilities
			});
		}

		// GET api/tenant/condotels/{id}/amenities - Lấy danh sách amenities của condotel (PHẢI ĐẶT TRƯỚC {id})
		[HttpGet("{id}/amenities")]
		[AllowAnonymous]
		public async Task<ActionResult<IEnumerable<AmenityDTO>>> GetCondotelAmenities(int id)
		{
			// Kiểm tra condotel có tồn tại không
			var condotelExists = await _context.Condotels.AnyAsync(c => c.CondotelId == id);
			if (!condotelExists)
				return NotFound(new { message = "Không tìm thấy condotel" });

			// Query trực tiếp từ database
			// Lấy tất cả amenities (bỏ filter Status để debug)
			var amenities = await _context.CondotelAmenities
				.Include(ca => ca.Amenity)
				.Where(ca => ca.CondotelId == id && ca.Amenity != null)
				.Select(ca => new AmenityDTO
				{
					AmenityId = ca.Amenity.AmenityId,
					Name = ca.Amenity.Name
				})
				.ToListAsync();

			return Ok(amenities);
		}

		// GET api/tenant/condotels/{id}/utilities - Lấy danh sách utilities của condotel (PHẢI ĐẶT TRƯỚC {id})
		[HttpGet("{id}/utilities")]
		[AllowAnonymous]
		public async Task<ActionResult<IEnumerable<UtilityDTO>>> GetCondotelUtilities(int id)
		{
			// Kiểm tra condotel có tồn tại không
			var condotelExists = await _context.Condotels.AnyAsync(c => c.CondotelId == id);
			if (!condotelExists)
				return NotFound(new { message = "Không tìm thấy condotel" });

			// Query trực tiếp từ database
			// Lấy tất cả utilities (bỏ filter Status để debug)
			var utilities = await _context.CondotelUtilities
				.Include(cu => cu.Utility)
				.Where(cu => cu.CondotelId == id && cu.Utility != null)
				.Select(cu => new UtilityDTO
				{
					UtilityId = cu.Utility.UtilityId,
					Name = cu.Utility.Name
				})
				.ToListAsync();

			return Ok(utilities);
		}

		// GET api/tenant/condotels/{id}/service-packages - Lấy danh sách service packages của condotel
		[HttpGet("{id}/service-packages")]
		[AllowAnonymous]
		public async Task<IActionResult> GetServicePackagesByCondotel(int id)
		{
			var condotelExists = await _context.Condotels.AnyAsync(c => c.CondotelId == id);
			if (!condotelExists)
				return NotFound(new { message = "Condotel not found" });

			var servicePackages = await _servicePackageService.GetByCondotelAsync(id);
			return Ok(servicePackages);
		}

		// GET api/tenant/condotels/host/{hostId}?pageNumber=1&pageSize=10 - Lấy danh sách condotels của một host (Public API)
		[HttpGet("host/{hostId}")]
		[AllowAnonymous]
		public ActionResult<object> GetCondotelsByHostId(int hostId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
		{
			if (hostId <= 0)
				return BadRequest(new { message = "Host ID không hợp lệ" });

			// Validate pagination parameters
			if (pageNumber < 1)
				pageNumber = 1;
			if (pageSize < 1 || pageSize > 100)
				pageSize = 10; // Default 10, max 100

			var result = _condotelService.GetCondtelsByHostPaged(hostId, pageNumber, pageSize);
			
			// Chỉ trả về các condotel có status "Active" hoặc "Hoạt động"
			var activeCondotels = result.Items
				.Where(c => c.Status == "Active" || c.Status == "Hoạt động")
				.ToList();

			// Recalculate total count for active condotels only
			var activeTotalCount = result.TotalCount; // Repository already filters by Active status

			return Ok(new
			{
				success = true,
				data = activeCondotels,
				pagination = new
				{
					pageNumber = result.PageNumber,
					pageSize = result.PageSize,
					totalCount = activeTotalCount,
					totalPages = (int)Math.Ceiling((double)activeTotalCount / result.PageSize),
					hasPreviousPage = result.HasPreviousPage,
					hasNextPage = result.PageNumber < (int)Math.Ceiling((double)activeTotalCount / result.PageSize),
					hostId = hostId
				}
			});
		}

		// GET api/tenant/condotels/{id} - Lấy chi tiết condotel (ĐẶT CUỐI CÙNG)
		[HttpGet("{id}")]
		[AllowAnonymous]
		public ActionResult<CondotelDetailDTO> GetCondotelById(int id)
		{
			var condotel = _condotelService.GetCondotelById(id);
			if (condotel == null || condotel.Status == "Inactive")
				return NotFound(new { message = "Không tìm thấy condotel" });

			return Ok(condotel);
		}
	}
}
