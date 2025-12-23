using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace CondotelManagement.Controllers.Promotion
{
    [ApiController]
    [Route("api")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
		private readonly IHostService _hostService;

		public PromotionController(IPromotionService promotionService, IHostService hostService)
        {
            _promotionService = promotionService;
            _hostService = hostService;
        }

        // GET /api/promotion
        [HttpGet("promotions")]
        public async Task<ActionResult<IEnumerable<PromotionDTO>>> GetAll()
        {
            var promotions = await _promotionService.GetAllAsync();
            return Ok(promotions);
        }

        // GET /api/promotion/{id}
        [HttpGet("promotion/{id}")]
        public async Task<ActionResult<PromotionDTO>> GetById(int id)
        {
            var promotion = await _promotionService.GetByIdAsync(id);
            if (promotion == null)
                return NotFound(new { message = "Promotion not found" });

            return Ok(promotion);
        }

        // GET /api/promotion/condotel/{condotelId}
        [HttpGet("promotions/condotel/{condotelId}")]
        public async Task<ActionResult<IEnumerable<PromotionDTO>>> GetByCondotelId(int condotelId)
        {
            var promotions = await _promotionService.GetByCondotelIdAsync(condotelId);
            return Ok(promotions);
        }

		// GET /api/promotion
		[HttpGet("host/promotions")]
		public async Task<ActionResult<IEnumerable<PromotionDTO>>> GetAllByHost()
		{
			//current host login
			var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
			var promotions = await _promotionService.GetAllByHostAsync(hostId);
			return Ok(promotions);
		}

		// POST /api/promotion
		[HttpPost("host/promotion")]
        [Authorize(Roles = "Host")]
        public async Task<ActionResult<PromotionDTO>> Create([FromBody] PromotionCreateUpdateDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "Invalid promotion data" });

			var result = await _promotionService.CreateAsync(dto);

			if (!result.Success)
				return BadRequest(new { message = result.Message });

            return CreatedAtAction(nameof(GetById), "Promotion", new { id = result.Data.PromotionId }, result.Data);
        }

        // PUT /api/promotion/{id}
        [HttpPut("host/promotion/{id}")]
        [Authorize(Roles = "Host")]
        public async Task<ActionResult> Update(int id, [FromBody] PromotionCreateUpdateDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "Invalid promotion data" });
            
            var result = await _promotionService.UpdateAsync(id, dto);
			if (!result.Success)
				return BadRequest(new { message = result.Message });

			return Ok(new { message = "Promotion updated successfully" });
        }

        // DELETE /api/promotion/{id}
        [HttpDelete("host/promotion/{id}")]
        [Authorize(Roles = "Host")]
        public async Task<ActionResult> Delete(int id)
        {
            var success = await _promotionService.DeleteAsync(id);
            if (!success) return NotFound(new { message = "Promotion not found" });
            
            return Ok(new { message = "Promotion deleted successfully" });
        }
    }
}

