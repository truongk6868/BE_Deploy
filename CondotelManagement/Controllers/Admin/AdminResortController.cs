using CondotelManagement.DTOs;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/resorts")]
    [Authorize(Roles = "Admin")]
    public class AdminResortController : ControllerBase
    {
        private readonly IResortService _resortService;
        private readonly IUtilitiesService _utilitiesService;

        public AdminResortController(IResortService resortService, IUtilitiesService utilitiesService)
        {
            _resortService = resortService;
            _utilitiesService = utilitiesService;
        }

        /// <summary>
        /// Lấy tất cả resorts
        /// GET /api/admin/resorts
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResortDTO>>> GetAll()
        {
            var resorts = await _resortService.GetAllAsync();
            return Ok(new
            {
                success = true,
                data = resorts,
                total = resorts.Count()
            });
        }

        /// <summary>
        /// Lấy resort theo ID
        /// GET /api/admin/resorts/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ResortDTO>> GetById(int id)
        {
            var resort = await _resortService.GetByIdAsync(id);
            if (resort == null)
                return NotFound(new { success = false, message = "Resort not found" });
            
            return Ok(new { success = true, data = resort });
        }

        /// <summary>
        /// Lấy resorts theo LocationId
        /// GET /api/admin/resorts/location/{locationId}
        /// </summary>
        [HttpGet("location/{locationId}")]
        public async Task<ActionResult<IEnumerable<ResortDTO>>> GetByLocationId(int locationId)
        {
            var resorts = await _resortService.GetByLocationIdAsync(locationId);
            return Ok(new
            {
                success = true,
                data = resorts,
                total = resorts.Count()
            });
        }

        /// <summary>
        /// Tạo resort mới
        /// POST /api/admin/resorts
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ResortDTO>> Create([FromBody] ResortCreateUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var created = await _resortService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ResortId }, new
            {
                success = true,
                message = "Resort created successfully",
                data = created
            });
        }

        /// <summary>
        /// Cập nhật resort
        /// PUT /api/admin/resorts/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ResortCreateUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var updated = await _resortService.UpdateAsync(id, dto);
            if (!updated)
                return NotFound(new { success = false, message = "Resort not found" });
            
            return Ok(new { success = true, message = "Resort updated successfully" });
        }

        /// <summary>
        /// Xóa resort
        /// DELETE /api/admin/resorts/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _resortService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { success = false, message = "Resort not found" });
            
            return Ok(new { success = true, message = "Resort deleted successfully" });
        }

        /// <summary>
        /// Lấy danh sách utilities của resort
        /// GET /api/admin/resorts/{resortId}/utilities
        /// </summary>
        [HttpGet("{resortId}/utilities")]
        public async Task<IActionResult> GetUtilities(int resortId)
        {
            var utilities = await _utilitiesService.GetByResortAsync(resortId);
            return Ok(new
            {
                success = true,
                data = utilities,
                total = utilities.Count()
            });
        }

        /// <summary>
        /// Thêm utility vào resort
        /// POST /api/admin/resorts/{resortId}/utilities
        /// </summary>
        [HttpPost("{resortId}/utilities")]
        public async Task<IActionResult> AddUtility(int resortId, [FromBody] AddUtilityToResortDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var success = await _resortService.AddUtilityToResortAsync(resortId, dto);
            if (!success)
                return NotFound(new { success = false, message = "Resort or Utility not found, or utility already exists" });
            
            return Ok(new { success = true, message = "Utility added to resort successfully" });
        }

        /// <summary>
        /// Xóa utility khỏi resort
        /// DELETE /api/admin/resorts/{resortId}/utilities/{utilityId}
        /// </summary>
        [HttpDelete("{resortId}/utilities/{utilityId}")]
        public async Task<IActionResult> RemoveUtility(int resortId, int utilityId)
        {
            var success = await _resortService.RemoveUtilityFromResortAsync(resortId, utilityId);
            if (!success)
                return NotFound(new { success = false, message = "Resort or Utility not found, or utility not associated with resort" });
            
            return Ok(new { success = true, message = "Utility removed from resort successfully" });
        }
    }
}

