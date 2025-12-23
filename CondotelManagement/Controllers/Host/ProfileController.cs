using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Models;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Host
{
	[ApiController]
	[Route("api/host")]
	[Authorize(Roles = "Host")]
	public class ProfileController : ControllerBase
	{
		private readonly IHostService _HostProfileService;

		public ProfileController(IHostService HostProfileService)
		{
            _HostProfileService = HostProfileService;
		}

		[HttpGet("profile")]
		public async Task<IActionResult> GetProfile()
		{
			var result = await _HostProfileService.GetHostProfileAsync(User.GetUserId());

			if (result == null)
				return NotFound(ApiResponse<object>.Fail("Không tìm thấy host"));

			return Ok(ApiResponse<object>.SuccessResponse(result));
		}

		[HttpPut("profile")]
		public async Task<IActionResult> UpdateProfile([FromBody] UpdateHostProfileDTO dto)
		{
			if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Now))
			{
				ModelState.AddModelError("DateOfBirth", "Ngày sinh không được lớn hơn hiện tại.");
			}

			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}

			var result = await _HostProfileService.UpdateHostProfileAsync(User.GetUserId(), dto);
			if (!result)
				return BadRequest(ApiResponse<object>.Fail("Sửa không thành công"));

			return Ok(ApiResponse<object>.SuccessResponse("Sửa profile thành công"));
		}
	}
}
