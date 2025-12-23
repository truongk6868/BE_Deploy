using CondotelManagement.DTOs;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/resorts")]
    [Authorize(Roles = "Host")]
    public class ResortController : ControllerBase
    {
        private readonly IResortService _resortService;

        public ResortController(IResortService resortService)
        {
            _resortService = resortService;
        }

        // GET api/host/resorts - Lấy tất cả resorts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResortDTO>>> GetAll()
        {
            var resorts = await _resortService.GetAllAsync();
            return Ok(resorts);
        }

        // GET api/host/resorts/{id} - Lấy resort theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ResortDTO>> GetById(int id)
        {
            var resort = await _resortService.GetByIdAsync(id);
            if (resort == null) return NotFound(new { message = "Resort not found" });
            return Ok(resort);
        }

        // GET api/host/resorts/location/{locationId} - Lấy resorts theo Location ID
        [HttpGet("location/{locationId}")]
        public async Task<ActionResult<IEnumerable<ResortDTO>>> GetByLocationId(int locationId)
        {
            var resorts = await _resortService.GetByLocationIdAsync(locationId);
            return Ok(resorts);
        }
    }
}












