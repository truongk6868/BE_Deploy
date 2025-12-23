using CondotelManagement.DTOs.Amenity;
using CondotelManagement.Helpers;
using CondotelManagement.Services.Interfaces.Amenity;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers
{
    [ApiController]
    [Route("api/amenities")]
    public class AmenityController : ControllerBase
    {
        private readonly IAmenityService _amenityService;

        public AmenityController(IAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        // GET: /api/amenities
        // Lấy tất cả amenities (public endpoint)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var amenities = await _amenityService.GetAllAsync();
			return Ok(ApiResponse<object>.SuccessResponse(amenities, null));
		}

        // GET: /api/amenities/{id}
        // Lấy amenity theo ID (public endpoint)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var amenity = await _amenityService.GetByIdAsync(id);
            if (amenity == null)
				return NotFound(ApiResponse<object>.Fail("Không tìm thấy tiện nghi"));

			return Ok(ApiResponse<object>.SuccessResponse(amenity, null));
		}
    }
}


