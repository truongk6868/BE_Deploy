using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Host
{
	[ApiController]
	[Route("api/host/utility")]
	[Authorize(Roles = "Host")]
	public class UtilityController : ControllerBase
	{
		private readonly IUtilitiesService _service;

		public UtilityController(IUtilitiesService service)
		{
			_service = service;
		}

		// ===========================
		// Lấy tất cả Utilities active by resort
		// ===========================
		[HttpGet("resort/{resortId}")]
		[AllowAnonymous] // Cho phép public access để frontend có thể load utilities khi edit condotel
		public async Task<IActionResult> GetByResort(int resortId)
		{
			if (resortId <= 0)
				return BadRequest(ApiResponse<object>.Fail("Resort ID không hợp lệ"));

			var result = await _service.GetByResortAsync(resortId);
			return Ok(ApiResponse<object>.SuccessResponse(result));
		}
	}
}
