using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Models;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CondotelManagement.Controllers.Tenant
{
    [ApiController]
    [Route("api/vouchers")]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        /// <summary>
        /// Lấy danh sách voucher của user đang đăng nhập (hỗ trợ cả Tenant và Host vì Host cũng là User)
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Tenant,Host")]
        public async Task<IActionResult> GetMyVouchers()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user" });
            }

            var vouchers = await _voucherService.GetVouchersByUserIdAsync(userId);
            return Ok(new
            {
                success = true,
                data = vouchers,
                total = vouchers.Count()
            });
        }

        /// <summary>
        /// Lấy danh sách voucher theo condotel - chỉ hiển thị voucher công khai (không có UserId)
        /// hoặc voucher của chính user đang đăng nhập (nếu có auth)
        /// </summary>
        [HttpGet("condotel/{condotelId}")]
        public async Task<IActionResult> GetVouchersByCondotel(int condotelId)
        {
            // Lấy userId nếu user đã đăng nhập (optional)
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                currentUserId = userId;
            }

            var allVouchers = await _voucherService.GetVouchersByCondotelAsync(condotelId);
            
            // Lọc: chỉ hiển thị voucher công khai HOẶC voucher của chính user đang đăng nhập
            var filteredVouchers = allVouchers.Where(v => 
                !v.UserID.HasValue || // Voucher công khai (không có UserID)
                (currentUserId.HasValue && v.UserID.Value == currentUserId.Value) // Hoặc voucher của chính user này
            );

            return Ok(ApiResponse<object>.SuccessResponse(filteredVouchers));
        }

		[HttpPost("auto-create/{bookingId}")]
		public async Task<IActionResult> CreateVoucherAfterBooking(int bookingId)
		{
			if (bookingId <= 0)
				return BadRequest(ApiResponse<object>.Fail("BookingID không hợp lệ."));

			var vouchers = await _voucherService.CreateVoucherAfterBookingAsync(bookingId);

			if (vouchers == null || vouchers.Count == 0)
				return Ok(ApiResponse<object>.Fail("Không thể tạo voucher – có thể host tắt AutoGenerate hoặc setting chưa cấu hình."));
			return Ok(ApiResponse<object>.SuccessResponse(vouchers, $"Đã tạo {vouchers.Count} voucher cho user."));
		}
	}
}
