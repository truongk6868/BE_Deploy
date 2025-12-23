using Microsoft.AspNetCore.Mvc;
using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Booking;
using CondotelManagement.Services.Interfaces.BookingService;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CondotelManagement.Data;
using Microsoft.EntityFrameworkCore;
using CondotelManagement.DTOs.Payment;
using CondotelManagement.Services.Interfaces.Payment;

namespace CondotelManagement.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly CondotelDbVer1Context _context = new CondotelDbVer1Context();
        private readonly IPayOSService _paymentService;

        public BookingController(IBookingService bookingService, CondotelDbVer1Context  context, IPayOSService payOS)
        {
            _bookingService = bookingService;
            _context = context;
            _paymentService = payOS;
        }

        private int GetCustomerId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
        [HttpPost("dev/fake-webhook/{bookingId}")]
        public async Task<IActionResult> FakeWebhook(int bookingId)
        {
            var fakeWebhook = new PayOSWebhookData
            {
                Code = "00",
                Success = true,
                Data = new PayOSWebhookPaymentData
                {
                    OrderCode = bookingId * 1_000_000,
                    Code = "PAID",
                    Amount = 1000000,
                    Currency = "VND"
                },
                Signature = "DEV"
            };

            var result = await _paymentService.ProcessWebhookAsync(fakeWebhook);

            return Ok(new
            {
                bookingId,
                webhookProcessed = result
            });
        }

        [HttpPost("dev/confirm-booking/{bookingId}")]
        public async Task<IActionResult> DevConfirmBooking(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return NotFound();

            booking.Status = "Confirmed";
            booking.CheckInToken = TokenHelper.GenerateCheckInToken(booking.BookingId);

            booking.CheckInTokenGeneratedAt = DateTime.UtcNow;
            booking.CheckInTokenUsedAt = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                booking.BookingId,
                booking.Status,
                booking.CheckInToken
            });
        }


        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings()
        {
            int customerId = GetCustomerId();
            var bookings = await _bookingService.GetBookingsByCustomerAsync(customerId);
            return Ok(bookings);
        }

        // GET api/booking/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null) return NotFound();
            return Ok(booking);
        }

        // GET api/booking/check-availability?condotelId=1&checkIn=2025-11-10&checkOut=2025-11-15
        [HttpGet("check-availability")]
        public IActionResult CheckAvailability(int condotelId, DateOnly checkIn, DateOnly checkOut)
        {
            bool available = _bookingService.CheckAvailability(condotelId, checkIn, checkOut);
            return Ok(new { condotelId, checkIn, checkOut, available });
        }

        // POST api/booking
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDTO dto)
        {
           
            var customerId = GetCustomerId();
            if (customerId <= 0)
                return Unauthorized("Không tìm thấy thông tin user.");

            var result = await _bookingService.CreateBookingAsync(dto, customerId);

            if (!result.Success)
                return BadRequest(result);

            
            // Ép kiểu data về BookingDTO
            var booking = (BookingDTO)result.Data;

            return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, result);
        }
        public class CheckInOfflineDTO
        {
            public string Token { get; set; }
            public string FullName { get; set; }
        }

        [HttpPost("check-in/offline")]
        public async Task<IActionResult> CheckInOffline(CheckInOfflineDTO dto)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.CheckInToken == dto.Token);

            if (booking == null)
                return NotFound("Token không hợp lệ");

            var expectedName = string.IsNullOrEmpty(booking.GuestFullName)
                ? booking.Customer.FullName
                : booking.GuestFullName;

            if (!expectedName.Trim().Equals(dto.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Tên người check-in không khớp");

            booking.Status = "InStay";
            booking.CheckInTokenUsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok("Check-in thành công");
        }




        // PUT api/booking/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateBooking(int id, [FromBody] BookingDTO dto)
        {
            // 1. Validate null body (phải làm trước)
            if (dto == null)
                return BadRequest("Request body trống.");

            // 2. Validate id trùng với body
            if (id != dto.BookingId)
                return BadRequest("Booking ID không khớp với URL.");

            // 3. Validate logic nghiệp vụ nhẹ
            if (dto.StartDate > dto.EndDate)
                return BadRequest("Ngày bắt đầu phải trước ngày kết thúc.");

            // 4. Validate không cho sửa các field nhạy cảm
            if (dto.CustomerId <= 0 || dto.CondotelId <= 0)
                return BadRequest("Không được chỉnh sửa customerId hoặc condotelId.");

            try
            {
                var updated = _bookingService.UpdateBooking(dto);

                if (updated == null)
                    return NotFound("Không tìm thấy booking.");

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                // Các lỗi nghiệp vụ: Completed, Cancelled, đã check-in, status sai...
                return BadRequest(ex.Message);
            }
        }


        // POST api/booking/{id}/refund - Đặt trước DELETE để tránh route conflict
        [HttpPost("{id}/refund")]
        public async Task<IActionResult> RefundBooking(int id, [FromBody] RefundBookingRequestDTO? request = null)
        {
            int customerId = GetCustomerId();
            
            // Log request để debug
            Console.WriteLine($"[RefundBooking] BookingId: {id}, CustomerId: {customerId}");
            if (request != null)
            {
                Console.WriteLine($"[RefundBooking] BankCode: {request.BankCode}, AccountNumber: {request.AccountNumber}, AccountHolder: {request.AccountHolder}");
            }
            else
            {
                Console.WriteLine("[RefundBooking] Request body is null");
            }
            
            var result = await _bookingService.RefundBooking(id, customerId, request?.BankCode, request?.AccountNumber, request?.AccountHolder);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE api/booking/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            int customerId = GetCustomerId();
            
            // Kiểm tra booking có tồn tại và thuộc về user không
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null || booking.CustomerId != customerId)
            {
                return NotFound(new { success = false, message = "Booking not found or you don't have permission to cancel this booking." });
            }
            
            // Nếu booking đã thanh toán (Confirmed), thử refund trước
            // Booking "Completed" không thể hủy và hoàn tiền
            if (booking.Status == "Completed")
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Không thể hủy booking. Booking đã hoàn thành (đã check-out), không thể hủy và hoàn tiền." 
                });
            }
            
            if (booking.Status == "Confirmed")
            {
                // Gọi trực tiếp RefundBooking để nhận message chi tiết
                var refundResult = await _bookingService.RefundBooking(id, customerId);
                if (!refundResult.Success)
                {
                    // Trả về message cụ thể từ RefundBooking
                    return BadRequest(new { 
                        success = false, 
                        message = refundResult.Message ?? "Không thể hủy booking. Vui lòng kiểm tra lại điều kiện hoàn tiền." 
                    });
                }
                
                return Ok(new { 
                    success = true, 
                    message = refundResult.Message ?? "Booking đã được hủy và yêu cầu hoàn tiền đã được gửi đến admin.",
                    data = refundResult.Data
                });
            }
            
            // Nếu booking chưa thanh toán, chỉ cần cancel
            try
            {
                var success = await _bookingService.CancelBooking(id, customerId);
                if (!success)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Failed to cancel booking. Please try again or contact support." 
                    });
                }
                
                return Ok(new { 
                    success = true, 
                    message = "Booking cancelled successfully." 
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { 
                    success = false, 
                    message = ex.Message 
                });
            }
        }

        // POST api/booking/{id}/cancel-payment - Hủy thanh toán (KHÔNG refund)
        [HttpPost("{id}/cancel-payment")]
        public async Task<IActionResult> CancelPayment(int id)
        {
            int customerId = GetCustomerId();
            var success = await _bookingService.CancelPayment(id, customerId);
            if (!success) 
                return BadRequest(new { success = false, message = "Cannot cancel payment. Booking may have already been paid or cancelled." });
            return Ok(new { success = true, message = "Payment cancelled successfully. Booking status updated to Cancelled." });
        }

        /// <summary>
        /// Kiểm tra xem booking có thể hoàn tiền được không (để hiển thị nút hoàn tiền)
        /// </summary>
        [HttpGet("{id}/can-refund")]
        public async Task<IActionResult> CanRefundBooking(int id)
        {
            int customerId = GetCustomerId();
            var canRefund = await _bookingService.CanRefundBooking(id, customerId);
            
            return Ok(new 
            { 
                success = true,
                canRefund = canRefund,
                message = canRefund 
                    ? "Booking can be refunded. Refund button should be displayed." 
                    : "Booking cannot be refunded. Refund button should be hidden."
            });
        }
    }
}
