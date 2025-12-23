using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
	[ApiController]
	[Route("api/admin/location")]
	[Authorize(Roles = "Admin")]
	public class AdminLocationController : ControllerBase
	{
		private readonly ILocationService _locationService;

		public AdminLocationController(ILocationService locationService)
		{
			_locationService = locationService;
		}

		[HttpGet("all")]
		public async Task<ActionResult<IEnumerable<LocationDTO>>> GetAll()
		{
			var locations = await _locationService.GetAllAsync();
			return Ok(ApiResponse<object>.SuccessResponse(locations));
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<LocationDTO>> GetById(int id)
		{
			var location = await _locationService.GetByIdAsync(id);
			if (location == null) return NotFound();
			return Ok(ApiResponse<object>.SuccessResponse(location));
		}

		[HttpPost]
		public async Task<ActionResult<LocationDTO>> Create(LocationCreateUpdateDTO dto)
		{
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}
			var created = await _locationService.CreateAsync(dto);
			return Ok(ApiResponse<object>.SuccessResponse(created, "Tạo địa điểm thành công"));
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, LocationCreateUpdateDTO dto)
		{
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}
			var updated = await _locationService.UpdateAsync(id, dto);
			if (!updated) return NotFound();
			return Ok(ApiResponse<object>.SuccessResponse("Sửa địa điểm thành công"));
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var deleted = await _locationService.DeleteAsync(id);
			if (!deleted) return NotFound();
			return Ok(ApiResponse<object>.SuccessResponse("Xóa địa điểm thành công"));
		}
	}
}

