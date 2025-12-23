using CondotelManagement.DTOs;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Tenant
{
	[ApiController]
	[Route("api/tenant/utilities")]
	public class UtilityController : ControllerBase
	{
		private readonly IUtilitiesService _utilitiesService;

		public UtilityController(IUtilitiesService utilitiesService)
		{
			_utilitiesService = utilitiesService;
		}

		/// <summary>
		/// Lấy danh sách utilities theo resort (Public API)
		/// GET /api/tenant/utilities/resort/{resortId}
		/// </summary>
		[HttpGet("resort/{resortId}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetByResort(int resortId)
		{
			if (resortId <= 0)
				return BadRequest(new { success = false, message = "Resort ID không hợp lệ" });

			var utilities = await _utilitiesService.GetByResortAsync(resortId);
			
			return Ok(new
			{
				success = true,
				data = utilities,
				total = utilities.Count()
			});
		}

		/// <summary>
		/// Lấy tất cả utilities (Public API)
		/// GET /api/tenant/utilities
		/// </summary>
		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetAll()
		{
			var utilities = await _utilitiesService.GetAllAsync();
			
			return Ok(new
			{
				success = true,
				data = utilities,
				total = utilities.Count()
			});
		}
	}
}






