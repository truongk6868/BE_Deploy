using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CondotelManagement.Controllers
{
	[ApiController]
	[Route("api/host/vouchers")]
	[Authorize(Roles = "Host")]
	public class VoucherController : ControllerBase
	{
		private readonly IVoucherService _voucherService;
		private readonly IHostService _hostService;

		public VoucherController(IVoucherService voucherService, IHostService hostService)
		{
			_voucherService = voucherService;
			_hostService = hostService;
		}

		[HttpGet]
		public async Task<IActionResult> GetVouchersByHost()
		{
			//current host login
			var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
			var vouchers = await _voucherService.GetVouchersByHostAsync(hostId);
			return Ok(ApiResponse<object>.SuccessResponse(vouchers));
		}

		[HttpPost]
		public async Task<IActionResult> CreateVoucher([FromBody] VoucherCreateDTO dto)
		{
		if (dto.StartDate >= dto.EndDate)
		{
			ModelState.AddModelError("StartDate", "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");
			ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
		}
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}
			var created = await _voucherService.CreateVoucherAsync(dto);
			return Ok(ApiResponse<object>.SuccessResponse(created, "Đã tạo thành công"));
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateVoucher(int id, [FromBody] VoucherCreateDTO dto)
		{
		if (dto.StartDate >= dto.EndDate)
		{
			ModelState.AddModelError("StartDate", "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");
			ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
		}
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}
			var updated = await _voucherService.UpdateVoucherAsync(id, dto);
			if (updated == null) return NotFound();
			return Ok(ApiResponse<object>.SuccessResponse("Đã sửa thành công"));
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteVoucher(int id)
		{
			var success = await _voucherService.DeleteVoucherAsync(id);
			if (!success) return NotFound();
			return Ok(ApiResponse<object>.SuccessResponse("Đã xóa thành công"));
		}
	}
}
