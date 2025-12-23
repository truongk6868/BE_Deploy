using CondotelManagement.Data;
using CondotelManagement.DTOs.Host;
using CondotelManagement.Services.Interfaces.Host;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/payout")]
    [Authorize(Roles = "Host")]
    public class PayoutController : ControllerBase
    {
        private readonly IHostPayoutService _payoutService;
        private readonly CondotelDbVer1Context _context;

        public PayoutController(IHostPayoutService payoutService, CondotelDbVer1Context context)
        {
            _payoutService = payoutService;
            _context = context;
        }


        // POST api/host/payout/process/{bookingId}
        // Xử lý payout cho một booking cụ thể
        [HttpPost("process/{bookingId}")]
        public async Task<IActionResult> ProcessPayoutForBooking(int bookingId)
        {
            try
            {
                var result = await _payoutService.ProcessPayoutForBookingAsync(bookingId);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing payout", error = ex.Message });
            }
        }

        // GET api/host/payout/pending
        // Lấy danh sách booking chờ thanh toán
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPayouts([FromQuery] int? hostId = null)
        {
            try
            {
                // Nếu là Host, chỉ lấy của mình
                if (User.IsInRole("Host") && !User.IsInRole("Admin"))
                {
                    // Lấy hostId từ user đang đăng nhập
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                        return Unauthorized(new { message = "Invalid user" });
                    
                    var host = await _context.Hosts.FirstOrDefaultAsync(h => h.UserId == userId);
                    if (host == null)
                        return Unauthorized(new { message = "Host not found" });
                    hostId = host.HostId;
                }

                var pendingPayouts = await _payoutService.GetPendingPayoutsAsync(hostId);
                return Ok(pendingPayouts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting pending payouts", error = ex.Message });
            }
        }
    }
}

