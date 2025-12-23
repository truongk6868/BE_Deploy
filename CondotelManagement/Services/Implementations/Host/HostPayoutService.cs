using CondotelManagement.Data;
using CondotelManagement.DTOs.Host;
using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces.Host;
using CondotelManagement.Services.Interfaces.Shared;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services.Implementations.Host
{
    public class HostPayoutService : IHostPayoutService
    {
        private readonly CondotelDbVer1Context _context;
        private readonly IEmailService _emailService;

        public HostPayoutService(CondotelDbVer1Context context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Helper method để check status "Completed" (hỗ trợ cả tiếng Anh và tiếng Việt)
        private bool IsCompletedStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var trimmedStatus = status.Trim();
            return trimmedStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                   trimmedStatus.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<HostPayoutResponseDTO> ProcessPayoutsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var cutoffDate = today.AddDays(-15); // 15 ngày trước

            // Lấy các booking đã completed >= 15 ngày, chưa được trả tiền, chưa bị từ chối
            var allEligibleBookings = await _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.Host)
                        .ThenInclude(h => h.User)
                .Include(b => b.Customer)
                .Where(b => b.EndDate <= cutoffDate
                    && (b.IsPaidToHost == null || b.IsPaidToHost == false)
                    && b.PayoutRejectedAt == null // Loại bỏ booking đã bị từ chối
                    && b.TotalPrice.HasValue
                    && b.TotalPrice.Value > 0)
                .ToListAsync();
            
            // Filter completed status (hỗ trợ cả tiếng Anh và tiếng Việt)
            var eligibleBookings = allEligibleBookings
                .Where(b => IsCompletedStatus(b.Status))
                .ToList();

            // Kiểm tra xem có refund request nào không
            var bookingIds = eligibleBookings.Select(b => b.BookingId).ToList();
            var refundRequests = await _context.RefundRequests
                .Where(r => bookingIds.Contains(r.BookingId) 
                    && (r.Status == "Pending" || r.Status == "Approved"))
                .Select(r => r.BookingId)
                .ToListAsync();

            // Loại bỏ các booking có refund request
            var bookingsToProcess = eligibleBookings
                .Where(b => !refundRequests.Contains(b.BookingId))
                .ToList();

            var processedItems = new List<HostPayoutItemDTO>();
            decimal totalAmount = 0;

            foreach (var booking in bookingsToProcess)
            {
                // Lấy thông tin tài khoản ngân hàng của host từ Wallet
                // Ưu tiên wallet default hoặc active để thực hiện thanh toán
                var hostWallet = await _context.Wallets
                    .Where(w => w.HostId == booking.Condotel.HostId && w.Status == "Active")
                    .OrderByDescending(w => w.IsDefault)
                    .FirstOrDefaultAsync();

                // Đánh dấu đã trả tiền
                booking.IsPaidToHost = true;
                booking.PaidToHostAt = DateTime.UtcNow;

                var daysSinceCompleted = (today.ToDateTime(TimeOnly.MinValue) - booking.EndDate.ToDateTime(TimeOnly.MinValue)).Days;

                // Đảm bảo Customer được load
                if (booking.Customer == null && booking.CustomerId > 0)
                {
                    booking.Customer = await _context.Users.FindAsync(booking.CustomerId);
                }

                processedItems.Add(new HostPayoutItemDTO
                {
                    BookingId = booking.BookingId,
                    CondotelId = booking.CondotelId,
                    CondotelName = booking.Condotel?.Name ?? "N/A",
                    HostId = booking.Condotel?.HostId ?? 0,
                    HostName = !string.IsNullOrWhiteSpace(booking.Condotel?.Host?.CompanyName) 
                        ? booking.Condotel.Host.CompanyName 
                        : (!string.IsNullOrWhiteSpace(booking.Condotel?.Host?.User?.FullName) 
                            ? booking.Condotel.Host.User.FullName 
                            : "N/A"),
                    Amount = booking.TotalPrice ?? 0m,
                    EndDate = booking.EndDate,
                    PaidAt = booking.PaidToHostAt,
                    IsPaid = true,
                    DaysSinceCompleted = daysSinceCompleted,
                    // Thông tin khách hàng
                    CustomerId = booking.CustomerId,
                    CustomerName = !string.IsNullOrWhiteSpace(booking.Customer?.FullName) 
                        ? booking.Customer.FullName 
                        : "Khách hàng",
                    CustomerEmail = booking.Customer?.Email,
                    // Thông tin tài khoản ngân hàng của host (để thực hiện thanh toán)
                    BankName = hostWallet?.BankName,
                    AccountNumber = hostWallet?.AccountNumber,
                    AccountHolderName = hostWallet?.AccountHolderName
                });

                totalAmount += booking.TotalPrice ?? 0m;

                // Gửi email thông báo cho host
                try
                {
                    var hostEmail = booking.Condotel.Host?.User?.Email;
                    if (!string.IsNullOrEmpty(hostEmail))
                    {
                        await _emailService.SendPayoutConfirmationEmailAsync(
                            toEmail: hostEmail,
                            hostName: booking.Condotel.Host?.CompanyName ?? "Host",
                            bookingId: booking.BookingId,
                            condotelName: booking.Condotel.Name,
                            amount: booking.TotalPrice ?? 0m,
                            paidAt: booking.PaidToHostAt.Value,
                            bankName: hostWallet?.BankName,
                            accountNumber: hostWallet?.AccountNumber,
                            accountHolderName: hostWallet?.AccountHolderName
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không dừng quá trình xử lý
                    Console.WriteLine($"[PayoutService] Error sending email to host: {ex.Message}");
                }
            }

            if (processedItems.Any())
            {
                await _context.SaveChangesAsync();
            }

            return new HostPayoutResponseDTO
            {
                Success = true,
                Message = $"Đã xử lý {processedItems.Count} booking và trả {totalAmount:N0} VNĐ cho host.",
                ProcessedCount = processedItems.Count,
                TotalAmount = totalAmount,
                ProcessedItems = processedItems
            };
        }

        public async Task<HostPayoutResponseDTO> ProcessPayoutForBookingAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.Host)
                        .ThenInclude(h => h.User)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Booking not found."
                };
            }

            if (!IsCompletedStatus(booking.Status))
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Booking must be completed to process payout."
                };
            }

            if (booking.IsPaidToHost == true)
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Booking has already been paid to host."
                };
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var cutoffDate = today.AddDays(-15);

            if (booking.EndDate > cutoffDate)
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = $"Booking must be completed for at least 15 days. EndDate: {booking.EndDate:yyyy-MM-dd}, Required: {cutoffDate:yyyy-MM-dd}"
                };
            }

            // Kiểm tra có refund request không
            var hasRefundRequest = await _context.RefundRequests
                .AnyAsync(r => r.BookingId == bookingId 
                    && (r.Status == "Pending" || r.Status == "Approved"));

            if (hasRefundRequest)
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Cannot process payout. Booking has pending or approved refund request."
                };
            }

            // Lấy thông tin tài khoản ngân hàng của host từ Wallet
            // Ưu tiên wallet default hoặc active để thực hiện thanh toán
            var hostWallet = await _context.Wallets
                .Where(w => w.HostId == booking.Condotel.HostId && w.Status == "Active")
                .OrderByDescending(w => w.IsDefault)
                .FirstOrDefaultAsync();

            // Đánh dấu đã trả tiền
            booking.IsPaidToHost = true;
            booking.PaidToHostAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Đảm bảo Customer được load
            if (booking.Customer == null && booking.CustomerId > 0)
            {
                booking.Customer = await _context.Users.FindAsync(booking.CustomerId);
            }

            var daysSinceCompleted = (today.ToDateTime(TimeOnly.MinValue) - booking.EndDate.ToDateTime(TimeOnly.MinValue)).Days;

            // Gửi email thông báo cho host
            try
            {
                var hostEmail = booking.Condotel.Host?.User?.Email;
                if (!string.IsNullOrEmpty(hostEmail))
                {
                    await _emailService.SendPayoutConfirmationEmailAsync(
                        toEmail: hostEmail,
                        hostName: booking.Condotel.Host?.CompanyName ?? "Host",
                        bookingId: booking.BookingId,
                        condotelName: booking.Condotel.Name,
                        amount: booking.TotalPrice ?? 0m,
                        paidAt: booking.PaidToHostAt.Value,
                        bankName: hostWallet?.BankName,
                        accountNumber: hostWallet?.AccountNumber,
                        accountHolderName: hostWallet?.AccountHolderName
                    );
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không dừng quá trình xử lý
                Console.WriteLine($"[PayoutService] Error sending email to host: {ex.Message}");
            }

            return new HostPayoutResponseDTO
            {
                Success = true,
                Message = $"Đã trả {booking.TotalPrice:N0} VNĐ cho host.",
                ProcessedCount = 1,
                TotalAmount = booking.TotalPrice ?? 0m,
                ProcessedItems = new List<HostPayoutItemDTO>
                {
                    new HostPayoutItemDTO
                    {
                        BookingId = booking.BookingId,
                        CondotelId = booking.CondotelId,
                        CondotelName = booking.Condotel?.Name ?? "N/A",
                        HostId = booking.Condotel?.HostId ?? 0,
                        HostName = !string.IsNullOrWhiteSpace(booking.Condotel?.Host?.CompanyName) 
                            ? booking.Condotel.Host.CompanyName 
                            : (!string.IsNullOrWhiteSpace(booking.Condotel?.Host?.User?.FullName) 
                                ? booking.Condotel.Host.User.FullName 
                                : "N/A"),
                        Amount = booking.TotalPrice ?? 0m,
                        EndDate = booking.EndDate,
                        PaidAt = booking.PaidToHostAt,
                        IsPaid = true,
                        DaysSinceCompleted = daysSinceCompleted,
                        // Thông tin khách hàng
                        CustomerId = booking.CustomerId,
                        CustomerName = !string.IsNullOrWhiteSpace(booking.Customer?.FullName) 
                            ? booking.Customer.FullName 
                            : "Khách hàng",
                        CustomerEmail = booking.Customer?.Email,
                        // Thông tin tài khoản ngân hàng của host (để thực hiện thanh toán)
                        BankName = hostWallet?.BankName,
                        AccountNumber = hostWallet?.AccountNumber,
                        AccountHolderName = hostWallet?.AccountHolderName
                    }
                }
            };
        }

        /// <summary>
        /// Admin báo lỗi thông tin tài khoản khi chuyển tiền thủ công
        /// Gửi email thông báo cho host về lỗi thông tin tài khoản
        /// </summary>
        public async Task<HostPayoutResponseDTO> ReportAccountErrorAsync(int bookingId, string errorMessage)
        {
            var booking = await _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.Host)
                        .ThenInclude(h => h.User)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Booking not found."
                };
            }

            if (!IsCompletedStatus(booking.Status))
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Booking must be completed to report account error."
                };
            }

            // Lấy thông tin tài khoản hiện tại của host
            var hostWallet = await _context.Wallets
                .Where(w => w.HostId == booking.Condotel.HostId && w.Status == "Active")
                .OrderByDescending(w => w.IsDefault)
                .FirstOrDefaultAsync();

            // Gửi email thông báo lỗi cho host
            try
            {
                var hostEmail = booking.Condotel.Host?.User?.Email;
                if (!string.IsNullOrEmpty(hostEmail))
                {
                    await _emailService.SendPayoutAccountErrorEmailAsync(
                        toEmail: hostEmail,
                        hostName: booking.Condotel.Host?.CompanyName ?? booking.Condotel.Host?.User?.FullName ?? "Host",
                        bookingId: booking.BookingId,
                        condotelName: booking.Condotel.Name,
                        amount: booking.TotalPrice ?? 0m,
                        currentBankName: hostWallet?.BankName,
                        currentAccountNumber: hostWallet?.AccountNumber,
                        currentAccountHolderName: hostWallet?.AccountHolderName,
                        errorMessage: errorMessage
                    );
                }
                else
                {
                    return new HostPayoutResponseDTO
                    {
                        Success = false,
                        Message = "Host email not found. Cannot send error notification."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayoutService] Error sending account error email to host: {ex.Message}");
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = $"Failed to send error notification email: {ex.Message}"
                };
            }

            return new HostPayoutResponseDTO
            {
                Success = true,
                Message = $"Đã gửi thông báo lỗi thông tin tài khoản cho host qua email. Host cần cập nhật thông tin tài khoản để nhận thanh toán.",
                ProcessedCount = 0,
                TotalAmount = 0m,
                ProcessedItems = new List<HostPayoutItemDTO>()
            };
        }

        /// <summary>
        /// Admin từ chối thanh toán cho host
        /// Gửi email thông báo cho host về lý do từ chối
        /// </summary>
        public async Task<HostPayoutResponseDTO> RejectPayoutAsync(int bookingId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Reason is required for rejecting a payout."
                };
            }

            var booking = await _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.Host)
                        .ThenInclude(h => h.User)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Booking not found."
                };
            }

            if (!IsCompletedStatus(booking.Status))
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Booking must be completed to reject payout."
                };
            }

            if (booking.IsPaidToHost == true)
            {
                return new HostPayoutResponseDTO
                {
                    Success = false,
                    Message = "Cannot reject payout for a booking that has already been paid to host."
                };
            }

            // Đánh dấu booking đã bị từ chối thanh toán
            booking.PayoutRejectedAt = DateTime.UtcNow;
            booking.PayoutRejectionReason = reason;
            await _context.SaveChangesAsync();

            // Gửi email thông báo từ chối thanh toán cho host
            try
            {
                var hostEmail = booking.Condotel.Host?.User?.Email;
                if (!string.IsNullOrEmpty(hostEmail))
                {
                    var hostName = !string.IsNullOrWhiteSpace(booking.Condotel.Host?.CompanyName)
                        ? booking.Condotel.Host.CompanyName
                        : (!string.IsNullOrWhiteSpace(booking.Condotel.Host?.User?.FullName)
                            ? booking.Condotel.Host.User.FullName
                            : "Host");

                    await _emailService.SendPayoutRejectionEmailAsync(
                        toEmail: hostEmail,
                        hostName: hostName,
                        bookingId: booking.BookingId,
                        condotelName: booking.Condotel.Name,
                        amount: booking.TotalPrice ?? 0m,
                        reason: reason
                    );
                }
                else
                {
                    // Vẫn trả về success vì đã đánh dấu reject trong DB
                    Console.WriteLine($"[PayoutService] Host email not found for booking {bookingId}, but rejection has been recorded.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayoutService] Error sending payout rejection email to host: {ex.Message}");
                // Vẫn trả về success vì đã đánh dấu reject trong DB
            }

            return new HostPayoutResponseDTO
            {
                Success = true,
                Message = $"Đã từ chối thanh toán và gửi thông báo cho host qua email.",
                ProcessedCount = 0,
                TotalAmount = 0m,
                ProcessedItems = new List<HostPayoutItemDTO>()
            };
        }

        public async Task<List<HostPayoutItemDTO>> GetPendingPayoutsAsync(int? hostId = null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var cutoffDate = today.AddDays(-15);

            var query = _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.Host)
                        .ThenInclude(h => h.User)
                .Include(b => b.Customer)
                .Where(b => b.EndDate <= cutoffDate
                    && (b.IsPaidToHost == null || b.IsPaidToHost == false)
                    && b.PayoutRejectedAt == null // Loại bỏ booking đã bị từ chối
                    && b.TotalPrice.HasValue
                    && b.TotalPrice.Value > 0);

            if (hostId.HasValue)
            {
                query = query.Where(b => b.Condotel.HostId == hostId.Value);
            }

            var bookings = await query.ToListAsync();
            
            // Filter completed status (hỗ trợ cả tiếng Anh và tiếng Việt)
            bookings = bookings.Where(b => IsCompletedStatus(b.Status)).ToList();

            // Lấy danh sách booking có refund request
            var bookingIds = bookings.Select(b => b.BookingId).ToList();
            var refundRequests = await _context.RefundRequests
                .Where(r => bookingIds.Contains(r.BookingId)
                    && (r.Status == "Pending" || r.Status == "Approved"))
                .Select(r => r.BookingId)
                .ToListAsync();

            // Loại bỏ các booking có refund request
            var eligibleBookings = bookings
                .Where(b => !refundRequests.Contains(b.BookingId))
                .ToList();

            // Lấy tất cả hostIds để query wallet một lần
            var hostIds = eligibleBookings.Select(b => b.Condotel.HostId).Distinct().ToList();
            var hostWallets = await _context.Wallets
                .Where(w => hostIds.Contains(w.HostId ?? 0) && w.Status == "Active")
                .ToListAsync();

            return eligibleBookings.Select(b =>
            {
                var daysSinceCompleted = (today.ToDateTime(TimeOnly.MinValue) - b.EndDate.ToDateTime(TimeOnly.MinValue)).Days;
                
                // Tìm thông tin tài khoản ngân hàng của host từ Wallet (ưu tiên default)
                // Đây là thông tin để thực hiện thanh toán cho host
                var hostWallet = hostWallets
                    .Where(w => w.HostId == b.Condotel.HostId)
                    .OrderByDescending(w => w.IsDefault)
                    .FirstOrDefault();

                return new HostPayoutItemDTO
                {
                    BookingId = b.BookingId,
                    CondotelId = b.CondotelId,
                    CondotelName = b.Condotel?.Name ?? "N/A",
                    HostId = b.Condotel?.HostId ?? 0,
                    HostName = !string.IsNullOrWhiteSpace(b.Condotel?.Host?.CompanyName) 
                        ? b.Condotel.Host.CompanyName 
                        : (!string.IsNullOrWhiteSpace(b.Condotel?.Host?.User?.FullName) 
                            ? b.Condotel.Host.User.FullName 
                            : "N/A"),
                    Amount = b.TotalPrice ?? 0m,
                    EndDate = b.EndDate,
                    PaidAt = null,
                    IsPaid = false,
                    DaysSinceCompleted = daysSinceCompleted,
                    // Thông tin khách hàng
                    CustomerId = b.CustomerId,
                    CustomerName = !string.IsNullOrWhiteSpace(b.Customer?.FullName) 
                        ? b.Customer.FullName 
                        : "Khách hàng",
                    CustomerEmail = b.Customer?.Email,
                    // Thông tin tài khoản ngân hàng của host (để thực hiện thanh toán)
                    BankName = hostWallet?.BankName,
                    AccountNumber = hostWallet?.AccountNumber,
                    AccountHolderName = hostWallet?.AccountHolderName
                };
            }).ToList();
        }

        public async Task<List<HostPayoutItemDTO>> GetPaidPayoutsAsync(int? hostId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.Host)
                        .ThenInclude(h => h.User)
                .Include(b => b.Customer)
                .Where(b => b.IsPaidToHost == true
                    && b.TotalPrice.HasValue
                    && b.TotalPrice.Value > 0);

            // Filter theo hostId nếu có
            if (hostId.HasValue)
            {
                query = query.Where(b => b.Condotel.HostId == hostId.Value);
            }

            // Filter theo ngày thanh toán nếu có
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.PaidToHostAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.PaidToHostAt <= toDate.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.PaidToHostAt)
                .ToListAsync();

            // Filter completed status (hỗ trợ cả tiếng Anh và tiếng Việt)
            bookings = bookings.Where(b => IsCompletedStatus(b.Status)).ToList();

            // Lấy tất cả hostIds để query wallet một lần
            var hostIds = bookings.Select(b => b.Condotel.HostId).Distinct().ToList();
            var hostWallets = await _context.Wallets
                .Where(w => hostIds.Contains(w.HostId ?? 0) && w.Status == "Active")
                .ToListAsync();

            return bookings.Select(b =>
            {
                var daysSinceCompleted = b.PaidToHostAt.HasValue && b.EndDate != null
                    ? (b.PaidToHostAt.Value.Date - b.EndDate.ToDateTime(TimeOnly.MinValue).Date).Days
                    : 0;

                // Tìm thông tin tài khoản ngân hàng của host từ Wallet (ưu tiên default)
                // Đây là thông tin đã được sử dụng để thực hiện thanh toán cho host
                var hostWallet = hostWallets
                    .Where(w => w.HostId == b.Condotel.HostId)
                    .OrderByDescending(w => w.IsDefault)
                    .FirstOrDefault();

                return new HostPayoutItemDTO
                {
                    BookingId = b.BookingId,
                    CondotelId = b.CondotelId,
                    CondotelName = b.Condotel?.Name ?? "N/A",
                    HostId = b.Condotel?.HostId ?? 0,
                    HostName = !string.IsNullOrWhiteSpace(b.Condotel?.Host?.CompanyName) 
                        ? b.Condotel.Host.CompanyName 
                        : (!string.IsNullOrWhiteSpace(b.Condotel?.Host?.User?.FullName) 
                            ? b.Condotel.Host.User.FullName 
                            : "N/A"),
                    Amount = b.TotalPrice ?? 0m,
                    EndDate = b.EndDate,
                    PaidAt = b.PaidToHostAt,
                    IsPaid = true,
                    DaysSinceCompleted = daysSinceCompleted,
                    // Thông tin khách hàng
                    CustomerId = b.CustomerId,
                    CustomerName = !string.IsNullOrWhiteSpace(b.Customer?.FullName) 
                        ? b.Customer.FullName 
                        : "Khách hàng",
                    CustomerEmail = b.Customer?.Email,
                    // Thông tin tài khoản ngân hàng của host (đã được sử dụng để thực hiện thanh toán)
                    BankName = hostWallet?.BankName,
                    AccountNumber = hostWallet?.AccountNumber,
                    AccountHolderName = hostWallet?.AccountHolderName
                };
            }).ToList();
        }

        public async Task<List<HostPayoutItemDTO>> GetRejectedPayoutsAsync(int? hostId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.Host)
                        .ThenInclude(h => h.User)
                .Include(b => b.Customer)
                .Where(b => b.PayoutRejectedAt != null // Chỉ lấy booking đã bị từ chối
                    && b.TotalPrice.HasValue
                    && b.TotalPrice.Value > 0);

            // Filter theo hostId nếu có
            if (hostId.HasValue)
            {
                query = query.Where(b => b.Condotel.HostId == hostId.Value);
            }

            // Filter theo ngày từ chối nếu có
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.PayoutRejectedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.PayoutRejectedAt <= toDate.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.PayoutRejectedAt) // Sắp xếp theo ngày từ chối (mới nhất trước)
                .ToListAsync();

            // Filter completed status (hỗ trợ cả tiếng Anh và tiếng Việt)
            bookings = bookings.Where(b => IsCompletedStatus(b.Status)).ToList();

            // Lấy tất cả hostIds để query wallet một lần
            var hostIds = bookings.Select(b => b.Condotel.HostId).Distinct().ToList();
            var hostWallets = await _context.Wallets
                .Where(w => hostIds.Contains(w.HostId ?? 0) && w.Status == "Active")
                .ToListAsync();

            return bookings.Select(b =>
            {
                var daysSinceCompleted = b.EndDate != null
                    ? (DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue) - b.EndDate.ToDateTime(TimeOnly.MinValue)).Days
                    : 0;

                // Tìm thông tin tài khoản ngân hàng của host từ Wallet (ưu tiên default)
                var hostWallet = hostWallets
                    .Where(w => w.HostId == b.Condotel.HostId)
                    .OrderByDescending(w => w.IsDefault)
                    .FirstOrDefault();

                return new HostPayoutItemDTO
                {
                    BookingId = b.BookingId,
                    CondotelId = b.CondotelId,
                    CondotelName = b.Condotel?.Name ?? "N/A",
                    HostId = b.Condotel?.HostId ?? 0,
                    HostName = !string.IsNullOrWhiteSpace(b.Condotel?.Host?.CompanyName) 
                        ? b.Condotel.Host.CompanyName 
                        : (!string.IsNullOrWhiteSpace(b.Condotel?.Host?.User?.FullName) 
                            ? b.Condotel.Host.User.FullName 
                            : "N/A"),
                    Amount = b.TotalPrice ?? 0m,
                    EndDate = b.EndDate,
                    PaidAt = null,
                    IsPaid = false,
                    DaysSinceCompleted = daysSinceCompleted,
                    // Thông tin khách hàng
                    CustomerId = b.CustomerId,
                    CustomerName = !string.IsNullOrWhiteSpace(b.Customer?.FullName) 
                        ? b.Customer.FullName 
                        : "Khách hàng",
                    CustomerEmail = b.Customer?.Email,
                    // Thông tin tài khoản ngân hàng của host
                    BankName = hostWallet?.BankName,
                    AccountNumber = hostWallet?.AccountNumber,
                    AccountHolderName = hostWallet?.AccountHolderName,
                    // Thêm thông tin từ chối
                    RejectedAt = b.PayoutRejectedAt,
                    RejectionReason = b.PayoutRejectionReason
                };
            }).ToList();
        }
    }
}

