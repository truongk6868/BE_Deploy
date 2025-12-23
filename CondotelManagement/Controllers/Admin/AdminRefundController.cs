using CondotelManagement.DTOs.Admin;
using CondotelManagement.Services.Interfaces.BookingService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/refund-requests")]
    public class AdminRefundController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public AdminRefundController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu hoàn tiền với filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRefundRequests(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = "all",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? condotelTypeId = null)
        {
            var refundRequests = await _bookingService.GetRefundRequestsAsync(searchTerm, status, startDate, endDate, condotelTypeId);

            return Ok(new
            {
                success = true,
                data = refundRequests,
                total = refundRequests.Count
            });
        }

        /// <summary>
        /// Xác nhận đã chuyển tiền thủ công (không dùng PayOS API tự động)
        /// </summary>
        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmRefund(int id)
        {
            var result = await _bookingService.ConfirmRefundManually(id);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Từ chối yêu cầu hoàn tiền
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectRefund(int id, [FromBody] RejectRefundRequestDTO? request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { success = false, message = "Reason is required for rejecting a refund request." });
            }

            var result = await _bookingService.RejectRefundRequest(id, request.Reason);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}






