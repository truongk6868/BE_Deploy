using CondotelManagement.DTOs.Amenity;
using CondotelManagement.Helpers;
using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Services.Interfaces.Amenity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/amenities")]
    [Authorize(Roles = "Host")]
    public class HostAmenityController : ControllerBase
    {
        private readonly IAmenityService _amenityService;
		private readonly IHostService _hostService;

		public HostAmenityController(IAmenityService amenityService, IHostService hostService)
        {
            _amenityService = amenityService;
            _hostService = hostService;
        }

        // GET: /api/host/amenities
        // Lấy tất cả amenities
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
				//current host login
				var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
				var amenities = await _amenityService.GetAllAsync(hostId);
                return Ok(ApiResponse<object>.SuccessResponse(amenities,null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Đã xảy ra lỗi khi tải tiện ích: " + ex.Message));
            }
        }

        // GET: /api/host/amenities/{id}
        // Lấy amenity theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var amenity = await _amenityService.GetByIdAsync(id);
                if (amenity == null)
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy tiện nghi"));

                return Ok(ApiResponse<object>.SuccessResponse(amenity, null));
            }
            catch (Exception ex)
            {
				return StatusCode(500, ApiResponse<object>.Fail("Đã xảy ra lỗi khi tải tiện ích: " + ex.Message));
			}
        }

        // GET: /api/host/amenities/by-category/{category}
        // Lấy amenities theo category
        [HttpGet("by-category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            try
            {
				//current host login
				var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
				var allAmenities = await _amenityService.GetAllAsync(hostId);
                var filtered = allAmenities.Where(a => 
                    !string.IsNullOrWhiteSpace(a.Category) && 
                    a.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                
                return Ok(ApiResponse<object>.SuccessResponse(filtered, null));
            }
            catch (Exception ex)
            {
				return StatusCode(500, ApiResponse<object>.Fail("Đã xảy ra lỗi khi tải tiện ích: " + ex.Message));
			}
        }

        // POST: /api/host/amenities
        // Tạo amenity mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AmenityRequestDTO dto)
        {
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}

			try
            {
				//current host login
				var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
				var created = await _amenityService.CreateAsync(hostId,dto);
                return Ok(ApiResponse<object>.SuccessResponse(created, "Tạo tiện ích thành công"));
            }
            catch (ArgumentException ex)
            {
				return BadRequest(ApiResponse<object>.Fail(ex.Message));
			}
            catch (Exception ex)
            {
				return StatusCode(500, ApiResponse<object>.Fail("Đã xảy ra lỗi khi tải tiện ích: " + ex.Message));
			}
        }

        // PUT: /api/host/amenities/{id}
        // Cập nhật amenity
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AmenityRequestDTO dto)
        {
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}

			try
            {
                var success = await _amenityService.UpdateAsync(id, dto);
                if (!success)
                    return NotFound(new { message = "Không tìm thấy tiện nghi" });

                return Ok(ApiResponse<object>.SuccessResponse("Tiện ích đã được cập nhật thành công"));
            }
            catch (ArgumentException ex)
            {
				return BadRequest(ApiResponse<object>.Fail(ex.Message));
			}
            catch (Exception ex)
            {
				return StatusCode(500, ApiResponse<object>.Fail("Đã xảy ra lỗi khi cập nhật tiện ích: " + ex.Message));
			}
        }

        // DELETE: /api/host/amenities/{id}
        // Xóa amenity
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _amenityService.DeleteAsync(id);
                if (!success)
                    return NotFound(ApiResponse<object>.Fail("Không thể tìm thấy tiện ích hoặc không thể xóa"));

                return Ok(ApiResponse<object>.SuccessResponse("Tiện ích đã được xóa thành công"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Đã xảy ra lỗi khi xóa tiện ích: " + ex.Message));
            }
        }
    }
}
