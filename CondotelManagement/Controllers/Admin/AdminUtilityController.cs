using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
	[ApiController]
	[Route("api/admin/utility")]
	[Authorize(Roles = "Admin")]
	public class AdminUtilityController : ControllerBase
	{
		private readonly IUtilitiesService _service;

		public AdminUtilityController(IUtilitiesService service)
		{
			_service = service;
		}

		// ===========================
		// GET: /api/utility/all
		// Lấy tất cả Utilities
		// ===========================
		[HttpGet("all")]
		public async Task<IActionResult> GetAll()
		{
			var result = await _service.GetAllAsync();
			return Ok(ApiResponse<object>.SuccessResponse(result));
		}

		// ===========================
		// GET: /api/utility/5
		// Lấy Utility theo ID 
		// ===========================
		[HttpGet("{utilityId}")]
		public async Task<IActionResult> GetById(int utilityId)
		{
			var result = await _service.GetByIdAsync(utilityId);

			if (result == null)
				return NotFound(ApiResponse<object>.Fail("Tiện ích không tồn tại"));

			return Ok(ApiResponse<object>.SuccessResponse(result));
		}

		// ===========================
		// POST: /api/utility
		// tạo Utility
		// ===========================
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] UtilityRequestDTO dto)
		{
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}

			var created = await _service.CreateAsync(dto);
			return Ok(ApiResponse<object>.SuccessResponse(created, "Đã tạo thành công"));
		}

		// ===========================
		// PUT: /api/utility/5/
		// update Utility
		// ===========================
		[HttpPut("{utilityId}")]
		public async Task<IActionResult> Update(int utilityId, [FromBody] UtilityRequestDTO dto)
		{
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}

			var success = await _service.UpdateAsync(utilityId, dto);

			if (!success)
				return NotFound(ApiResponse<object>.Fail("Tiện ích không tồn tại"));

			return Ok(ApiResponse<object>.SuccessResponse("Đã sửa thành công"));
		}

		// ===========================
		// DELETE: /api/utility/5
		// xóa Utility
		// ===========================
		[HttpDelete("{utilityId}")]
		public async Task<IActionResult> Delete(int utilityId)
		{
			var success = await _service.DeleteAsync(utilityId);

			if (!success)
				return NotFound(ApiResponse<object>.Fail("Tiện ích không tồn tại"));

			return Ok(ApiResponse<object>.SuccessResponse("Đã xóa thành công"));
		}
	}
}



