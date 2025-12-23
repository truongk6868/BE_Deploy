using CondotelManagement.DTOs.Admin;
using CondotelManagement.DTOs.Host;
using CondotelManagement.Services.Interfaces.Host;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CondotelManagement.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/payouts")]
    public class AdminPayoutController : ControllerBase
    {
        private readonly IHostPayoutService _payoutService;

        public AdminPayoutController(IHostPayoutService payoutService)
        {
            _payoutService = payoutService;
        }

        /// <summary>
        /// Lấy danh sách booking chờ thanh toán cho host (Admin xem tất cả)
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPayouts([FromQuery] int? hostId = null)
        {
            try
            {
                var pendingPayouts = await _payoutService.GetPendingPayoutsAsync(hostId);
                return Ok(new
                {
                    success = true,
                    data = pendingPayouts,
                    total = pendingPayouts.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi nhận khoản thanh toán đang chờ xử lý", error = ex.Message });
            }
        }

        /// <summary>
        /// Xử lý tất cả các booking đủ điều kiện thanh toán cho host
        /// </summary>
        [HttpPost("process-all")]
        public async Task<IActionResult> ProcessAllPayouts()
        {
            try
            {
                var result = await _payoutService.ProcessPayoutsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi xử lý thanh toán", error = ex.Message });
            }
        }

        /// <summary>
        /// Xác nhận và xử lý payout cho một booking cụ thể
        /// </summary>
        [HttpPost("{bookingId}/confirm")]
        public async Task<IActionResult> ConfirmPayout(int bookingId)
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
                return StatusCode(500, new { message = "Lỗi xác nhận thanh toán", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin báo lỗi thông tin tài khoản khi chuyển tiền thủ công
        /// Gửi email thông báo cho host về lỗi thông tin tài khoản
        /// </summary>
        [HttpPost("{bookingId}/report-account-error")]
        public async Task<IActionResult> ReportAccountError(int bookingId, [FromBody] ReportAccountErrorRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ErrorMessage))
                {
                    return BadRequest(new { success = false, message = "Error message is required." });
                }

                var result = await _payoutService.ReportAccountErrorAsync(bookingId, request.ErrorMessage);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reporting account error", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin từ chối thanh toán cho host
        /// Gửi email thông báo cho host về lý do từ chối
        /// </summary>
        [HttpPost("{bookingId}/reject")]
        public async Task<IActionResult> RejectPayout(int bookingId, [FromBody] RejectPayoutRequestDTO request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { success = false, message = "Reason is required for rejecting a payout." });
                }

                var result = await _payoutService.RejectPayoutAsync(bookingId, request.Reason);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error rejecting payout", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách booking đã thanh toán cho host (đã hoàn thành và đã trả tiền)
        /// GET /api/admin/payouts/paid?hostId=1&fromDate=2025-01-01&toDate=2025-12-31
        /// </summary>
        [HttpGet("paid")]
        public async Task<IActionResult> GetPaidPayouts(
            [FromQuery] int? hostId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var paidPayouts = await _payoutService.GetPaidPayoutsAsync(hostId, fromDate, toDate);
                
                var totalAmount = paidPayouts.Sum(p => p.Amount);
                
                return Ok(new
                {
                    success = true,
                    data = paidPayouts,
                    total = paidPayouts.Count,
                    totalAmount = totalAmount,
                    summary = new
                    {
                        totalBookings = paidPayouts.Count,
                        totalRevenue = totalAmount,
                        averageAmount = paidPayouts.Count > 0 ? totalAmount / paidPayouts.Count : 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi nhận được khoản thanh toán", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách booking đã bị từ chối thanh toán (Admin xem tất cả hoặc lọc theo host)
        /// GET /api/admin/payouts/rejected?hostId=1&fromDate=2025-01-01&toDate=2025-12-31
        /// </summary>
        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedPayouts(
            [FromQuery] int? hostId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var rejectedPayouts = await _payoutService.GetRejectedPayoutsAsync(hostId, fromDate, toDate);
                
                var totalAmount = rejectedPayouts.Sum(p => p.Amount);
                
                return Ok(new
                {
                    success = true,
                    data = rejectedPayouts,
                    total = rejectedPayouts.Count,
                    totalAmount = totalAmount,
                    summary = new
                    {
                        totalBookings = rejectedPayouts.Count,
                        totalAmount = totalAmount,
                        averageAmount = rejectedPayouts.Count > 0 ? totalAmount / rejectedPayouts.Count : 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách payout đã từ chối", error = ex.Message });
            }
        }
    }
}


