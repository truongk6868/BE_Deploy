using CondotelManagement.DTOs;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Tenant
{
	[ApiController]
	[Route("api/tenant/locations")]
	public class LocationController : ControllerBase
	{
		private readonly ILocationService _locationService;

		public LocationController(ILocationService locationService)
		{
			_locationService = locationService;
		}

		// GET api/tenant/locations - Lấy tất cả locations (public)
		[HttpGet]
		[AllowAnonymous]
		public async Task<ActionResult<IEnumerable<LocationDTO>>> GetAll()
		{
			var locations = await _locationService.GetAllAsync();
			return Ok(locations);
		}

		// GET api/tenant/locations/{id} - Lấy location theo ID (public)
		[HttpGet("{id}")]
		[AllowAnonymous]
		public async Task<ActionResult<LocationDTO>> GetById(int id)
		{
			var location = await _locationService.GetByIdAsync(id);
			if (location == null) 
				return NotFound(new { message = "Location not found" });
			return Ok(location);
		}

		// GET api/tenant/locations/search?keyword=abc - Search locations theo keyword (public)
		[HttpGet("search")]
		[AllowAnonymous]
		public async Task<ActionResult<IEnumerable<LocationDTO>>> Search([FromQuery] string? keyword)
		{
			var allLocations = await _locationService.GetAllAsync();
			
			if (string.IsNullOrWhiteSpace(keyword))
			{
				return Ok(allLocations);
			}

			var filtered = allLocations.Where(l => 
				(!string.IsNullOrEmpty(l.Name) && l.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
				(!string.IsNullOrEmpty(l.Description) && l.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
			);

			return Ok(filtered);
		}
	}
}


