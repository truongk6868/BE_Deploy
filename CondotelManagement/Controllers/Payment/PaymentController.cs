using CondotelManagement.DTOs.Payment;
using CondotelManagement.Services.Interfaces.Payment;
using CondotelManagement.Services.Interfaces.BookingService;
using CondotelManagement.Services;
using CondotelManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CondotelManagement.Controllers.Payment
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPayOSService _payOSService;
        private readonly CondotelDbVer1Context _context;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IPayOSService payOSService,
            CondotelDbVer1Context context,
            IConfiguration configuration)
        {
            _payOSService = payOSService;
            _context = context;
            _configuration = configuration;
        }

        // Backward compatibility endpoint
        [HttpPost("create")]
        [Authorize] // Cho phép bất kỳ user đã đăng nhập nào (sẽ check ownership bên trong)
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            return await CreatePayOSPayment(request);
        }

        [HttpPost("payos/create")]
        [Authorize] // Cho phép bất kỳ user đã đăng nhập nào (sẽ check ownership bên trong)
        public async Task<IActionResult> CreatePayOSPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                // Validate user
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Invalid user" });
                }

                // Get booking details
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Condotel)
                    .FirstOrDefaultAsync(b => b.BookingId == request.BookingId);

                if (booking == null)
                {
                    return NotFound(new { success = false, message = "Booking not found" });
                }

                if (booking.CustomerId != userId)
                {
                    return StatusCode(403, new { success = false, message = "Access denied. You can only create payment for your own bookings." });
                }

                if (booking.Status != "Pending")
                {
                    return BadRequest(new { success = false, message = "Booking is not in a payable state. Current status: " + booking.Status });
                }

                if (booking.TotalPrice == null || booking.TotalPrice <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid booking amount. TotalPrice: " + booking.TotalPrice });
                }

                // PayOS minimum amount is 10,000 VND
                if (booking.TotalPrice < 10000)
                {
                    return BadRequest(new { success = false, message = "Amount must be at least 10,000 VND. Current amount: " + booking.TotalPrice });
                }

                // Tạo OrderCode unique: BookingId * 1000000 + random 6 digits
                // Đảm bảo OrderCode unique và có thể extract BookingId từ OrderCode
                var random = new Random();
                var randomSuffix = random.Next(100000, 999999); // 6 chữ số ngẫu nhiên
                var orderCode = (long)request.BookingId * 1000000L + randomSuffix;

                Console.WriteLine($"Creating payment - BookingId: {request.BookingId}, OrderCode: {orderCode}, TotalPrice: {booking.TotalPrice}");

                // Create PayOS payment request
                var frontendUrl = _configuration["AppSettings:FrontendUrl"];
                var backendBaseUrl = _configuration["AppSettings:BackendBaseUrl"];

                // HARD FAIL nếu thiếu config (để không fallback localhost)
                if (string.IsNullOrWhiteSpace(frontendUrl))
                {
                    throw new InvalidOperationException("AppSettings:FrontendUrl is not configured");
                }

                if (string.IsNullOrWhiteSpace(backendBaseUrl))
                {
                    throw new InvalidOperationException("AppSettings:BackendBaseUrl is not configured");
                }

                frontendUrl = frontendUrl.TrimEnd('/');
                backendBaseUrl = backendBaseUrl.TrimEnd('/');
                // PayOS yêu cầu amount là VND (không phải cents)
                // Nhưng để đảm bảo, kiểm tra cả hai format
                var totalPriceVnd = (int)booking.TotalPrice.Value;
                var amount = totalPriceVnd; // Sử dụng VND trực tiếp

                // PayOS minimum amount is 10,000 VND
                if (amount < 10000)
                {
                    return BadRequest(new { success = false, message = "Amount must be at least 10,000 VND" });
                }

                // PayOS description must be <= 25 characters
                const int payOsDescriptionLimit = 25;
                var condotelName = booking.Condotel?.Name?.Trim();
                var baseDescription = !string.IsNullOrWhiteSpace(condotelName)
                    ? $"{condotelName} #{request.BookingId}"
                    : $"Condotel #{request.BookingId}";
                var description = baseDescription.Length > payOsDescriptionLimit
                    ? baseDescription.Substring(0, payOsDescriptionLimit)
                    : baseDescription;


                // Route PayOS callbacks back to API (it will redirect to frontend after processing)
                var returnUrl = $"{backendBaseUrl}/api/payment/payos/return";
                var cancelUrl = returnUrl;
                var payOSRequest = new PayOSCreatePaymentRequest
                {
                    OrderCode = orderCode,
                    Amount = amount,
                    Description = description,
                    BuyerName = !string.IsNullOrWhiteSpace(booking.Customer?.FullName) ? booking.Customer.FullName : null,
                    BuyerPhone = !string.IsNullOrWhiteSpace(booking.Customer?.Phone) ? booking.Customer.Phone : null,
                    BuyerEmail = !string.IsNullOrWhiteSpace(booking.Customer?.Email) ? booking.Customer.Email : null,
                    Items = new List<PayOSItem>
                    {
                        new PayOSItem
                        {
                            // Giữ nguyên ký tự tiếng Việt trong item name
                            Name = !string.IsNullOrWhiteSpace(booking.Condotel?.Name)
                                ? booking.Condotel.Name
                                : $"Đặt phòng #{request.BookingId}",
                            Quantity = 1,
                            Price = amount
                        }
                    },
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl,
                    ExpiredAt = null // Không gửi ExpiredAt
                };

                PayOSCreatePaymentResponse? response = null;
                int maxRetries = 3;
                int retryCount = 0;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        // Tạo payment link mới
                        response = await _payOSService.CreatePaymentLinkAsync(payOSRequest);

                        // Nếu thành công, break khỏi loop
                        if (response.Code == "00")
                        {
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException($"PayOS error: {response.Desc} (Code: {response.Code})");
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Code: 20") && retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        Console.WriteLine($"Code: 20 error received (attempt {retryCount}/{maxRetries}). Generating new orderCode...");

                        // Tạo orderCode mới với random suffix khác
                        var newRandom = new Random();
                        var newRandomSuffix = newRandom.Next(100000, 999999);
                        orderCode = (long)request.BookingId * 1000000L + newRandomSuffix;
                        payOSRequest.OrderCode = orderCode;

                        Console.WriteLine($"New OrderCode: {orderCode}");
                        await Task.Delay(1000); // Đợi một chút trước khi retry
                    }
                    catch (Exception ex)
                    {
                        // Nếu không phải Code: 20 hoặc đã hết retry, throw exception
                        throw;
                    }
                }

                // Kiểm tra response có giá trị
                if (response == null)
                {
                    throw new InvalidOperationException("Failed to create payment link after retries");
                }

                // Nếu vẫn lỗi sau khi retry
                if (response.Code != "00")
                {
                    throw new InvalidOperationException($"PayOS error: {response.Desc} (Code: {response.Code})");
                }

                if (response.Code == "00" && response.Data != null)
                {
                    // Store paymentLinkId and orderCode for future reference (optional - can be stored in database)
                    Console.WriteLine($"Payment link created - BookingId: {request.BookingId}, OrderCode: {response.Data.OrderCode}, PaymentLinkId: {response.Data.PaymentLinkId}");

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            checkoutUrl = response.Data.CheckoutUrl,
                            paymentLinkId = response.Data.PaymentLinkId,
                            qrCode = response.Data.QrCode,
                            amount = response.Data.Amount,
                            orderCode = response.Data.OrderCode,
                            bookingId = request.BookingId // Include bookingId for reference
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = response.Desc ?? "Failed to create payment link"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create PayOS Payment Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Check if it's a connection error
                if (ex.Message.Contains("Cannot connect to PayOS API") ||
                    ex.Message.Contains("timed out") ||
                    ex.InnerException is System.Net.Http.HttpRequestException)
                {
                    return StatusCode(503, new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message.Contains("PayOS") ? ex.Message : "Internal server error"
                });
            }
        }

        [HttpGet("payos/status/{paymentLinkId}")]
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> GetPaymentStatus(string paymentLinkId)
        {
            try
            {
                var paymentInfo = await _payOSService.GetPaymentInfoAsync(paymentLinkId);

                if (paymentInfo.Code == "00" && paymentInfo.Data != null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            status = paymentInfo.Data.Status,
                            amount = paymentInfo.Data.Amount,
                            amountPaid = paymentInfo.Data.AmountPaid,
                            amountRemaining = paymentInfo.Data.AmountRemaining,
                            orderCode = paymentInfo.Data.OrderCode,
                            transactions = paymentInfo.Data.Transactions
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = paymentInfo.Desc ?? "Failed to get payment status"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get Payment Status Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Check if it's a connection error
                if (ex.Message.Contains("Cannot connect to PayOS API") ||
                    ex.Message.Contains("timed out") ||
                    ex.InnerException is System.Net.Http.HttpRequestException)
                {
                    return StatusCode(503, new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message.Contains("PayOS") ? ex.Message : "Internal server error"
                });
            }
        }

        /// <summary>
        /// Handle PayOS Return URL callback
        /// PayOS redirects user to this URL after payment with query params:
        /// - code: "00" (success) or "01" (Invalid Params)
        /// - id: Payment Link Id (string)
        /// - cancel: "true" (cancelled) or "false" (paid/pending)
        /// - status: "PAID", "PENDING", "PROCESSING", "CANCELLED"
        /// - orderCode: Order code (number)
        /// </summary>
        [HttpGet("payos/return")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSReturnUrl(
            [FromQuery] string? code,
            [FromQuery] string? id,
            [FromQuery] string? cancel,
            [FromQuery] string? status,
            [FromQuery] long? orderCode)
        {
            try
            {
                Console.WriteLine($"PayOS Return URL called - code: {code}, id: {id}, cancel: {cancel}, status: {status}, orderCode: {orderCode}");

                // Khai báo frontendUrl một lần ở đầu method
                var frontendUrl = _configuration["AppSettings:FrontendUrl"]
                    ?? throw new InvalidOperationException("AppSettings:FrontendUrl is missing");

                // Validate required params
                if (orderCode == null)
                {
                    Console.WriteLine("Missing orderCode in Return URL");
                    return Redirect($"{frontendUrl}/payment-error?message=Missing orderCode");
                }

                // Check if code is valid (00 = success, 01 = Invalid Params)
                if (code == "01")
                {
                    Console.WriteLine($"PayOS returned error code: {code}");
                    return Redirect($"{frontendUrl}/payment-error?message=Invalid payment parameters");
                }

                // Extract BookingId from OrderCode
                // OrderCode format: BookingId * 1000000 + random 6 digits (hoặc 999999 cho refund)
                var orderCodeSuffix = orderCode.Value % 1000000;
                var isRefundPayment = orderCodeSuffix == 999999;
                var bookingId = (int)(orderCode.Value / 1000000);

                if (isRefundPayment)
                {
                    // === XỬ LÝ REFUND PAYMENT ===
                    var refundRequest = await _context.RefundRequests
                        .Include(r => r.Booking)
                        .FirstOrDefaultAsync(r => r.BookingId == bookingId && r.Status == "Pending");

                    if (refundRequest == null)
                    {
                        Console.WriteLine($"RefundRequest not found for booking {bookingId} and orderCode {orderCode}");
                        return Redirect($"{frontendUrl}/refund-error?message=Refund request not found");
                    }

                    if (status == "PAID" && cancel != "true")
                    {
                        // Refund payment successful - customer đã nhận tiền
                        refundRequest.Status = "Refunded";
                        refundRequest.ProcessedAt = DateTime.UtcNow;
                        refundRequest.UpdatedAt = DateTime.UtcNow;
                        // Booking status giữ nguyên "Cancelled", không đổi thành "Refunded"
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"RefundRequest {refundRequest.Id} (Booking {bookingId}) status updated to Refunded from Return URL (Payment Link Id: {id})");
                        return Redirect($"{frontendUrl}/refund-success?bookingId={bookingId}&status=success&orderCode={orderCode}&paymentLinkId={id}");
                    }
                    else if (status == "CANCELLED" || cancel == "true")
                    {
                        // Refund payment was cancelled
                        Console.WriteLine($"Refund payment cancelled for booking {bookingId} (Payment Link Id: {id})");
                        return Redirect($"{frontendUrl}/refund-cancel?bookingId={bookingId}&status=cancelled&orderCode={orderCode}&paymentLinkId={id}");
                    }
                    else if (status == "PENDING" || status == "PROCESSING")
                    {
                        // Refund payment is still pending/processing
                        Console.WriteLine($"Refund payment pending/processing for booking {bookingId} (Payment Link Id: {id})");
                        return Redirect($"{frontendUrl}/refund-pending?bookingId={bookingId}&status=pending&orderCode={orderCode}");
                    }
                    else
                    {
                        // Unknown status
                        Console.WriteLine($"Unknown refund payment status '{status}' for booking {bookingId}");
                        return Redirect($"{frontendUrl}/refund-pending?bookingId={bookingId}&status=unknown&orderCode={orderCode}");
                    }
                }

                // === XỬ LÝ BOOKING PAYMENT (BÌNH THƯỜNG) ===
                // Sử dụng transaction với ReadCommitted isolation (đủ để tránh race condition)
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                try
                {
                    // Lock booking row để tránh race condition
                    var booking = await _context.Bookings
                        .FromSqlRaw(
                            "SELECT * FROM Booking WITH (UPDLOCK, ROWLOCK) WHERE BookingId = @bookingId",
                            new Microsoft.Data.SqlClient.SqlParameter("@bookingId", bookingId))
                        .FirstOrDefaultAsync();

                    if (booking == null)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Booking {bookingId} not found for orderCode {orderCode}");
                        return Redirect($"{frontendUrl}/payment-error?message=Booking not found");
                    }

                    // Process based on status and cancel flag
                    // Status values: PAID, PENDING, PROCESSING, CANCELLED
                    // Cancel: "true" = cancelled, "false" = paid/pending

                    if (status == "PAID" && cancel != "true")
                    {
                        // Payment successful
                        if (booking.Status == "Confirmed" || booking.Status == "Completed")
                        {
                            // Đã confirm rồi → bỏ qua
                            await transaction.CommitAsync();
                            Console.WriteLine($"[Return URL] Booking {bookingId} đã xác nhận trước đó → bỏ qua");
                            return Redirect($"{frontendUrl}/pay-done?bookingId={bookingId}&status=success&orderCode={orderCode}&paymentLinkId={id}");
                        }

                        // Kiểm tra availability trước khi confirm để tránh double booking
                        var today = DateOnly.FromDateTime(DateTime.UtcNow);
                        var conflictingBookings = await _context.Bookings
                            .FromSqlRaw(@"
                                SELECT * FROM Booking WITH (UPDLOCK, ROWLOCK)
                                WHERE CondotelId = @condotelId 
                                AND BookingId != @currentBookingId
                                AND Status IN ('Confirmed', 'Completed', 'Pending')
                                AND Status != 'Cancelled'
                                AND EndDate >= @today",
                                new Microsoft.Data.SqlClient.SqlParameter("@condotelId", booking.CondotelId),
                                new Microsoft.Data.SqlClient.SqlParameter("@currentBookingId", booking.BookingId),
                                new Microsoft.Data.SqlClient.SqlParameter("@today", today))
                            .ToListAsync();

                        // Kiểm tra overlap với các booking đã confirmed/completed
                        var hasConflict = conflictingBookings
                            .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                            .Any(b => !(booking.EndDate <= b.StartDate || booking.StartDate >= b.EndDate));

                        if (hasConflict)
                        {
                            // Có conflict với booking đã confirmed/completed → không thể confirm
                            Console.WriteLine($"[Return URL] Booking {bookingId} có conflict với booking đã confirmed/completed → hủy booking");
                            booking.Status = "Cancelled";
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            
                            // TODO: Có thể gửi email thông báo cho customer về việc booking bị hủy do conflict
                            return Redirect($"{frontendUrl}/payment-error?message=Condotel không còn trống trong khoảng thời gian này. Đặt phòng đã bị hủy.");
                        }

                        // Không có conflict → confirm booking
                        booking.Status = "Confirmed";
                        
                        // Tạo CheckInToken nếu chưa có
                        if (string.IsNullOrEmpty(booking.CheckInToken))
                        {
                            booking.CheckInToken = TokenHelper.GenerateCheckInToken(booking.BookingId);
                            booking.CheckInTokenGeneratedAt = DateTime.Now;
                            booking.CheckInTokenUsedAt = null;
                        }
                        
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        Console.WriteLine($"[Return URL] Booking {bookingId} confirmed. Email & voucher handled by webhook.");
                        
                        // NOTE: Webhook sẽ xử lý email + voucher để tránh timeout
                        return Redirect($"{frontendUrl}/pay-done?bookingId={bookingId}&status=success&orderCode={orderCode}&paymentLinkId={id}");
                    }
                    else
                    {
                        await transaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"[Return URL] ========================================");
                    Console.WriteLine($"[Return URL] ERROR PROCESSING PAYMENT");
                    Console.WriteLine($"[Return URL] BookingId: {bookingId}");
                    Console.WriteLine($"[Return URL] OrderCode: {orderCode}");
                    Console.WriteLine($"[Return URL] Status: {status}");
                    Console.WriteLine($"[Return URL] Cancel: {cancel}");
                    Console.WriteLine($"[Return URL] Error Message: {ex.Message}");
                    Console.WriteLine($"[Return URL] Stack Trace: {ex.StackTrace}");
                    Console.WriteLine($"[Return URL] Inner Exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"[Return URL] ========================================");
                    var errorFrontendUrl = _configuration["AppSettings:FrontendUrl"]
                        ?? throw new InvalidOperationException("AppSettings:FrontendUrl is missing");
                    return Redirect($"{errorFrontendUrl}/payment-error?message={Uri.EscapeDataString(ex.Message)}");
                }

                // Xử lý các trường hợp khác (CANCELLED, PENDING, etc.)
                if (status == "CANCELLED" || cancel == "true")
                {
                    // Payment was cancelled - Hủy thanh toán (KHÔNG refund)
                    Console.WriteLine($"Payment cancelled for booking {bookingId} (Payment Link Id: {id})");

                    var bookingForCancel = await _context.Bookings
                        .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                    if (bookingForCancel == null)
                    {
                        return Redirect($"{frontendUrl}/payment-error?message=Booking not found");
                    }

                    if (bookingForCancel.Status != "Cancelled")
                    {
                        // Sử dụng CancelPayment để đảm bảo không refund
                        try
                        {
                            using var scope = HttpContext.RequestServices.CreateScope();
                            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                            var customerId = bookingForCancel.CustomerId;
                            var cancelResult = await bookingService.CancelPayment(bookingId, customerId);
                            
                            if (cancelResult)
                            {
                                Console.WriteLine($"Booking {bookingId} status updated to Cancelled from Return URL (Payment Link Id: {id}) - Payment cancelled, no refund");
                            }
                            else
                            {
                                // Fallback: set status manually nếu CancelPayment fail
                                bookingForCancel.Status = "Cancelled";
                                await _context.SaveChangesAsync();
                                Console.WriteLine($"Booking {bookingId} status updated to Cancelled (fallback) from Return URL");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error calling CancelPayment: {ex.Message}");
                            // Fallback: set status manually
                            bookingForCancel.Status = "Cancelled";
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"Booking {bookingId} status updated to Cancelled (fallback) from Return URL");
                        }
                    }

                    return Redirect($"{frontendUrl}/payment/cancel?bookingId={bookingId}&status=cancelled&orderCode={orderCode}&paymentLinkId={id}");
                }
                else if (status == "PENDING" || status == "PROCESSING")
                {
                    // Payment is still pending/processing
                    Console.WriteLine($"Payment pending/processing for booking {bookingId} (Payment Link Id: {id})");
                    return Redirect($"{frontendUrl}/checkout?bookingId={bookingId}&status=pending&orderCode={orderCode}");
                }
                else
                {
                    // Unknown status
                    Console.WriteLine($"Unknown payment status '{status}' for booking {bookingId}");
                    return Redirect($"{frontendUrl}/checkout?bookingId={bookingId}&status=unknown&orderCode={orderCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"========================================");
                Console.WriteLine($"PayOS Return URL OUTER ERROR");
                Console.WriteLine($"OrderCode: {orderCode}");
                Console.WriteLine($"Status: {status}");
                Console.WriteLine($"Cancel: {cancel}");
                Console.WriteLine($"Code: {code}");
                Console.WriteLine($"Id: {id}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"========================================");
                
                var outerErrorFrontendUrl = _configuration["AppSettings:FrontendUrl"]
                    ?? throw new InvalidOperationException("AppSettings:FrontendUrl is missing");
                return Redirect($"{outerErrorFrontendUrl}/payment-error?message={Uri.EscapeDataString(ex.Message)}&orderCode={orderCode}");
            }
        }

        [HttpPost("payos/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine($"PayOS Webhook received: {body}");

                // Log all headers
                foreach (var h in Request.Headers)
                {
                    Console.WriteLine($"Header: {h.Key} = {h.Value}");
                }

                // Get signature from header or body
                var signature = Request.Headers["signature"].FirstOrDefault()
                    ?? Request.Headers["Signature"].FirstOrDefault()
                    ?? Request.Headers["X-Signature"].FirstOrDefault();

                if (string.IsNullOrEmpty(signature))
                {
                    // Try to get from body
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("signature", out var sigProp))
                        {
                            signature = sigProp.GetString();
                        }
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(signature))
                {
                    Console.WriteLine("Missing signature in webhook (header + body)");
                    return BadRequest(new { message = "Missing signature" });
                }

                if (!_payOSService.VerifyWebhookSignature(signature, body))
                {
                    Console.WriteLine("Invalid webhook signature");
                    return BadRequest(new { message = "Invalid signature" });
                }

                // Parse webhook data
                // PayOS webhook format: camelCase properties
                var webhookData = JsonSerializer.Deserialize<PayOSWebhookData>(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (webhookData == null)
                {
                    Console.WriteLine("Invalid webhook data - failed to parse");
                    return BadRequest(new { message = "Invalid webhook data" });
                }

                Console.WriteLine($"Webhook parsed - Code: {webhookData.Code}, Desc: {webhookData.Desc}, Success: {webhookData.Success}, HasData: {webhookData.Data != null}");

                // Process webhook
                var processed = await _payOSService.ProcessWebhookAsync(webhookData);

                if (processed)
                {
                    return Ok(new { message = "Webhook processed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to process webhook" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS Webhook Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("payos/cancel/{paymentLinkId}")]
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> CancelPayment(string paymentLinkId, [FromBody] CancelPaymentRequest? request = null)
        {
            try
            {
                var result = await _payOSService.CancelPaymentLinkAsync(paymentLinkId, request?.Reason ?? "User cancelled");

                if (result.Code == "00")
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Payment cancelled successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Desc ?? "Failed to cancel payment"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancel Payment Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Check if it's a connection error
                if (ex.Message.Contains("Cannot connect to PayOS API") ||
                    ex.Message.Contains("timed out") ||
                    ex.InnerException is System.Net.Http.HttpRequestException)
                {
                    return StatusCode(503, new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message.Contains("PayOS") ? ex.Message : "Internal server error"
                });
            }
        }

        /// <summary>
        /// Test endpoint to verify PayOS connection
        /// </summary>
        [HttpGet("payos/test")]
        [AllowAnonymous]
        public IActionResult TestPayOSConnection()
        {
            try
            {
                var clientId = _configuration["PayOS:ClientId"];
                var apiKey = _configuration["PayOS:ApiKey"];
                var baseUrl = _configuration["PayOS:BaseUrl"];

                return Ok(new
                {
                    success = true,
                    message = "PayOS configuration loaded",
                    config = new
                    {
                        baseUrl = baseUrl,
                        clientIdConfigured = !string.IsNullOrEmpty(clientId),
                        apiKeyConfigured = !string.IsNullOrEmpty(apiKey),
                        clientIdPreview = clientId?.Substring(0, Math.Min(8, clientId?.Length ?? 0)) + "...",
                        apiKeyPreview = apiKey?.Substring(0, Math.Min(8, apiKey?.Length ?? 0)) + "..."
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("payos/cancel-by-booking/{bookingId}")]
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> CancelPaymentByBooking(int bookingId, [FromBody] CancelPaymentRequest? request = null)
        {
            try
            {
                // Validate user
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Invalid user" });
                }

                // Get booking
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    return NotFound(new { success = false, message = "Booking not found" });
                }

                if (booking.CustomerId != userId)
                {
                    return StatusCode(403, new { success = false, message = "Access denied. You can only cancel payment for your own bookings." });
                }

                // Try to cancel by OrderCode
                // Since OrderCode format is BookingId * 1000000 + random, we need to try different possible OrderCodes
                // Or better: PayOS might allow canceling by OrderCode range
                // For now, we'll try to cancel using the base OrderCode (BookingId * 1000000)
                // But this won't work if the random suffix is needed

                // Better approach: Try to get payment info using OrderCode pattern
                // Since we can't know exact OrderCode, we'll need to store it when creating payment
                // For now, return error asking for paymentLinkId

                return BadRequest(new
                {
                    success = false,
                    message = "Cannot cancel payment without exact OrderCode. Please use the paymentLinkId returned when creating the payment link, or use endpoint: POST /api/payment/payos/cancel/{paymentLinkId}"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancel Payment By Booking Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // Helper method to convert Vietnamese to ASCII
        private static string ConvertVietnameseToAscii(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            var result = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);

            // Replace specific Vietnamese characters
            var replacements = new Dictionary<string, string>
            {
                { "đ", "d" }, { "Đ", "D" },
                { "à", "a" }, { "á", "a" }, { "ạ", "a" }, { "ả", "a" }, { "ã", "a" },
                { "â", "a" }, { "ầ", "a" }, { "ấ", "a" }, { "ậ", "a" }, { "ẩ", "a" }, { "ẫ", "a" },
                { "ă", "a" }, { "ằ", "a" }, { "ắ", "a" }, { "ặ", "a" }, { "ẳ", "a" }, { "ẵ", "a" },
                { "è", "e" }, { "é", "e" }, { "ẹ", "e" }, { "ẻ", "e" }, { "ẽ", "e" },
                { "ê", "e" }, { "ề", "e" }, { "ế", "e" }, { "ệ", "e" }, { "ể", "e" }, { "ễ", "e" },
                { "ì", "i" }, { "í", "i" }, { "ị", "i" }, { "ỉ", "i" }, { "ĩ", "i" },
                { "ò", "o" }, { "ó", "o" }, { "ọ", "o" }, { "ỏ", "o" }, { "õ", "o" },
                { "ô", "o" }, { "ồ", "o" }, { "ố", "o" }, { "ộ", "o" }, { "ổ", "o" }, { "ỗ", "o" },
                { "ơ", "o" }, { "ờ", "o" }, { "ớ", "o" }, { "ợ", "o" }, { "ở", "o" }, { "ỡ", "o" },
                { "ù", "u" }, { "ú", "u" }, { "ụ", "u" }, { "ủ", "u" }, { "ũ", "u" },
                { "ư", "u" }, { "ừ", "u" }, { "ứ", "u" }, { "ự", "u" }, { "ử", "u" }, { "ữ", "u" },
                { "ỳ", "y" }, { "ý", "y" }, { "ỵ", "y" }, { "ỷ", "y" }, { "ỹ", "y" },
                { "À", "A" }, { "Á", "A" }, { "Ạ", "A" }, { "Ả", "A" }, { "Ã", "A" },
                { "Â", "A" }, { "Ầ", "A" }, { "Ấ", "A" }, { "Ậ", "A" }, { "Ẩ", "A" }, { "Ẫ", "A" },
                { "Ă", "A" }, { "Ằ", "A" }, { "Ắ", "A" }, { "Ặ", "A" }, { "Ẳ", "A" }, { "Ẵ", "A" },
                { "È", "E" }, { "É", "E" }, { "Ẹ", "E" }, { "Ẻ", "E" }, { "Ẽ", "E" },
                { "Ê", "E" }, { "Ề", "E" }, { "Ế", "E" }, { "Ệ", "E" }, { "Ể", "E" }, { "Ễ", "E" },
                { "Ì", "I" }, { "Í", "I" }, { "Ị", "I" }, { "Ỉ", "I" }, { "Ĩ", "I" },
                { "Ò", "O" }, { "Ó", "O" }, { "Ọ", "O" }, { "Ỏ", "O" }, { "Õ", "O" },
                { "Ô", "O" }, { "Ồ", "O" }, { "Ố", "O" }, { "Ộ", "O" }, { "Ổ", "O" }, { "Ỗ", "O" },
                { "Ơ", "O" }, { "Ờ", "O" }, { "Ớ", "O" }, { "Ợ", "O" }, { "Ở", "O" }, { "Ỡ", "O" },
                { "Ù", "U" }, { "Ú", "U" }, { "Ụ", "U" }, { "Ủ", "U" }, { "Ũ", "U" },
                { "Ư", "U" }, { "Ừ", "U" }, { "Ứ", "U" }, { "Ự", "U" }, { "Ử", "U" }, { "Ữ", "U" },
                { "Ỳ", "Y" }, { "Ý", "Y" }, { "Ỵ", "Y" }, { "Ỷ", "Y" }, { "Ỹ", "Y" }
            };

            foreach (var replacement in replacements)
            {
                result = result.Replace(replacement.Key, replacement.Value);
            }

            return result;
        }

        /// <summary>
        /// Tạo QR code để chuyển tiền với thông tin: tên, tài khoản, số tiền
        /// </summary>
        [HttpPost("generate-qr")]
        [AllowAnonymous]
        public IActionResult GenerateQRCode([FromBody] GenerateQRRequestDTO request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request body is required" });

            if (string.IsNullOrWhiteSpace(request.BankCode))
                return BadRequest(new { success = false, message = "Bank code is required" });

            if (string.IsNullOrWhiteSpace(request.AccountNumber))
                return BadRequest(new { success = false, message = "Account number is required" });

            if (request.Amount < 1000)
                return BadRequest(new { success = false, message = "Amount must be at least 1,000 VND" });

            if (string.IsNullOrWhiteSpace(request.AccountHolderName))
                return BadRequest(new { success = false, message = "Account holder name is required" });

            try
            {
                // Normalize bank code to uppercase
                var bankCode = request.BankCode.ToUpper().Trim();
                
                // Map common bank names to standard VietQR bin codes (acqId)
                // Reference: https://api.vietqr.io/v2/banks
                var bankToBinMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Short codes with correct bin codes
                    { "VCB", "970436" },        // Vietcombank
                    { "VIETCOMBANK", "970436" },
                    { "ICB", "970415" },        // VietinBank (Industrial and Commercial Bank of Vietnam)
                    { "CTG", "970415" },        // VietinBank (Cong Thuong)
                    { "VIETINBANK", "970415" },
                    { "TCB", "970407" },        // Techcombank
                    { "TECHCOMBANK", "970407" },
                    { "MB", "970422" },         // MBBank (Military Bank)
                    { "MBBANK", "970422" },
                    { "ACB", "970416" },        // ACB (Asia Commercial Bank)
                    { "BID", "970418" },        // BIDV (Bank for Investment and Development of Vietnam)
                    { "BIDV", "970418" },
                    { "VBA", "970405" },        // Agribank (Vietnam Bank for Agriculture and Rural Development)
                    { "AGRIBANK", "970405" },
                    { "STB", "970403" },        // Sacombank
                    { "SACOMBANK", "970403" },
                    { "VPB", "970432" },        // VPBank (Vietnam Prosperity Bank)
                    { "VPBANK", "970432" },
                    { "TPB", "970423" },        // TPBank (Tien Phong Bank)
                    { "TPBANK", "970423" },
                    { "HDB", "970437" },        // HDBank (Ho Chi Minh City Development Bank)
                    { "HDBANK", "970437" },
                    { "SHB", "970443" },        // SHB (Saigon-Hanoi Bank)
                    { "SHBBANK", "970443" },
                    { "EIB", "970431" },        // Eximbank (Vietnam Export Import Commercial Joint Stock Bank)
                    { "EXIMBANK", "970431" },
                    { "MSB", "970426" },        // MSB (Maritime Bank)
                    { "MSBANK", "970426" },
                    { "NAB", "970428" },        // NamABank
                    { "NAMABANK", "970428" },
                    { "VAB", "970427" },        // VietABank (Vietnam Asia Commercial Bank)
                    { "VIETABANK", "970427" },
                    { "PGB", "970430" },        // PGBank (Petrolimex Group Commercial Bank)
                    { "PGBANK", "970430" },
                    { "SEAB", "970440" },       // SeABank (Southeast Asia Commercial Bank)
                    { "SEABANK", "970440" },
                    { "ABB", "970425" },        // ABBank (An Binh Commercial Bank)
                    { "ABBANK", "970425" },
                    { "BAB", "970409" },        // BacABank (Bac A Commercial Bank)
                    { "BACABANK", "970409" },
                    { "KLB", "970452" },        // KienLongBank
                    { "KIENLONGBANK", "970452" },
                    { "NCB", "970419" },        // NCB (National Citizen Bank)
                    { "VB", "970433" },         // VietBank (Vietnam Thuong Tin Commercial Bank)
                    { "VIETBANK", "970433" },
                    { "LPB", "970449" },        // LienVietPostBank
                    { "LIENVIETPOSTBANK", "970449" },
                    { "PVB", "970412" },        // PVcomBank (Petro Vietnam Commercial Bank)
                    { "PVCOMBANK", "970412" },
                    { "OCB", "970448" },        // OCB (Orient Commercial Bank)
                    { "OCEANBANK", "970414" },  // OceanBank
                    { "OJB", "970414" },        
                    { "GPB", "970408" },        // GPBank (Global Petro Bank)
                    { "GPBANK", "970408" },
                    { "SCB", "970429" },        // SCB (Sai Gon Commercial Bank)
                    { "SAIGONBANK", "970429" },
                    { "CAKE", "546034" },       // Cake by VPBank
                    { "UBANK", "546035" },      // Ubank by VPBank
                    { "TIMO", "963388" },       // Timo by Ban Viet Bank
                    { "VCCB", "970454" },       // VietCapital Bank
                    { "VIETCAPITALBANK", "970454" }
                };
                
                // Try to map to bin code first
                string binCode;
                if (bankToBinMap.ContainsKey(bankCode))
                {
                    binCode = bankToBinMap[bankCode];
                }
                else
                {
                    // If not found in map, assume it's already a bin code or valid bank code
                    binCode = bankCode;
                }
                
                // Tạo nội dung chuyển khoản
                var content = string.IsNullOrWhiteSpace(request.Content) 
                    ? "Chuyen tien" 
                    : request.Content;

                // Encode các tham số
                var encodedContent = Uri.EscapeDataString(content);
                var encodedAccountName = Uri.EscapeDataString(request.AccountHolderName);

                // Tạo URL QR code từ VietQR với bin code
                // Format: https://img.vietqr.io/image/{binCode}-{accountNumber}-{template}.jpg?amount={amount}&addInfo={content}&accountName={accountName}
                var baseUrl = "https://img.vietqr.io/image";
                var qrCodeUrlCompact = $"{baseUrl}/{binCode}-{request.AccountNumber}-compact.jpg?amount={request.Amount}&addInfo={encodedContent}&accountName={encodedAccountName}";
                var qrCodeUrlPrint = $"{baseUrl}/{binCode}-{request.AccountNumber}-print.jpg?amount={request.Amount}&addInfo={encodedContent}&accountName={encodedAccountName}";

                var response = new GenerateQRResponseDTO
                {
                    QrCodeUrl = qrCodeUrlCompact, // Default URL
                    QrCodeUrlCompact = qrCodeUrlCompact,
                    QrCodeUrlPrint = qrCodeUrlPrint,
                    BankCode = binCode, // Return bin code
                    AccountNumber = request.AccountNumber,
                    Amount = request.Amount,
                    AccountHolderName = request.AccountHolderName,
                    Content = content
                };

                return Ok(new
                {
                    success = true,
                    message = "QR code generated successfully",
                    data = response
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generate QR Code Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating QR code: " + ex.Message
                });
            }
        }

        // 1. SỬA DTO: OrderCode là string
        public class CreatePackagePaymentRequest
        {
            public string OrderCode { get; set; } = string.Empty;  // ← STRING LUÔN!
            public int Amount { get; set; }
            public string? Description { get; set; }
        }

        [HttpPost("create-package-payment")]
        [Authorize]
        public async Task<IActionResult> CreatePackagePayment([FromBody] CreatePackagePaymentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OrderCode) || request.Amount <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                // FIX DUY NHẤT – XỬ LÝ ORDERCODE LỚN CHÍNH XÁC 100%
                long orderCodeLong;
                if (!long.TryParse(request.OrderCode.Trim(), System.Globalization.NumberStyles.None, null, out orderCodeLong))
                {
                    // Nếu là dạng khoa học (e+24), ép về string nguyên rồi parse lại
                    var cleanOrderCode = new string(request.OrderCode.Where(char.IsDigit).ToArray());
                    if (!long.TryParse(cleanOrderCode, out orderCodeLong))
                    {
                        return BadRequest(new { success = false, message = "OrderCode không hợp lệ" });
                    }
                }
                // ĐẾN ĐÂY LÀ ORDERCODE ĐÃ LÀ LONG CHÍNH XÁC 100%!!!

                var description = string.IsNullOrWhiteSpace(request.Description)
                    ? "Nâng cấp gói dịch vụ"
                    : request.Description.Length > 25 ? request.Description.Substring(0, 25) : request.Description;

                var frontendUrl = _configuration["AppSettings:FrontendUrl"]
                    ?? throw new InvalidOperationException("AppSettings:FrontendUrl is missing");
                var returnUrl = $"{frontendUrl}/payment/success?type=package";
                var cancelUrl = $"{frontendUrl}/pricing";

                var payOSRequest = new PayOSCreatePaymentRequest
                {
                    OrderCode = orderCodeLong,  // BÂY GIỜ ĐÃ AN TOÀN HOÀN TOÀN
                    Amount = request.Amount,
                    Description = description,
                    Items = new List<PayOSItem>
        {
            new PayOSItem { Name = description, Quantity = 1, Price = request.Amount }
        },
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl
                };

                var response = await _payOSService.CreatePaymentLinkAsync(payOSRequest);

                if (response?.Code == "00" && response.Data != null)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Tạo link thanh toán thành công!",
                        data = new
                        {
                            checkoutUrl = response.Data.CheckoutUrl,
                            qrCode = response.Data.QrCode ?? "",
                            orderCode = response.Data.OrderCode,
                            paymentLinkId = response.Data.PaymentLinkId
                        }
                    });
                }

                return BadRequest(new { success = false, message = response?.Desc ?? "Lỗi từ PayOS" });
            }
            catch (FormatException ex)
            {
                // ← Bắt riêng lỗi parse để dễ debug
                Console.WriteLine($"[CreatePackagePayment] OrderCode không hợp lệ: {request.OrderCode}");
                return StatusCode(500, new { success = false, message = "OrderCode không hợp lệ" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreatePackagePayment] ERROR: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi tạo link thanh toán" });
            }
        }
    }


        public class CreatePaymentRequest
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "BookingId is required")]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "BookingId must be greater than 0")]
        public int BookingId { get; set; }
    }

    public class CancelPaymentRequest
    {
        public string? Reason { get; set; }
    }
}
