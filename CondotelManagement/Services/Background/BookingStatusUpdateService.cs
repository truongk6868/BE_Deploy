using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CondotelManagement.Data;
using CondotelManagement.Services.Interfaces.Shared;

namespace CondotelManagement.Services.Background
{
    public class BookingStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingStatusUpdateService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Chạy mỗi 5 phút để kịp thời

        public BookingStatusUpdateService(
            IServiceProvider serviceProvider,
            ILogger<BookingStatusUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[BookingStatusUpdate] Service is starting...");

            // Chờ 10 giây để app khởi động hoàn tất
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"[BookingStatusUpdate] Running scheduled check at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                    // Chuyển Confirmed → InStay (sau 14:00 ngày check-in)
                    await UpdateConfirmedToInStayAsync(stoppingToken);

                    // Chuyển InStay → Completed (sau 12:00 ngày check-out)
                    await UpdateInStayToCompletedAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BookingStatusUpdate] Error occurred while updating booking statuses");
                }

                // Đợi đến lần chạy tiếp theo
                _logger.LogInformation($"[BookingStatusUpdate] Next check in {_interval.TotalMinutes} minutes");

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("[BookingStatusUpdate] Service cancellation requested");
                    break;
                }
            }

            _logger.LogInformation("[BookingStatusUpdate] Service is stopping...");
        }

        /// <summary>
        /// Chuyển trạng thái Confirmed → InStay sau 14:00 ngày check-in
        /// </summary>
        private async Task UpdateConfirmedToInStayAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[BookingStatusUpdate] Checking for bookings to move from Confirmed to InStay...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CondotelDbVer1Context>();

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            // Tìm booking: Status = Confirmed, StartDate = today, và đã qua 14:00
            var bookingsToCheckIn = await context.Bookings
                .Where(b => b.Status == "Confirmed"
                         && b.StartDate == today
                         && currentTime >= new TimeOnly(14, 0)) // Đã qua 14:00
                .ToListAsync(cancellationToken);

            // Hoặc những booking có StartDate < today nhưng vẫn Confirmed (quá hạn check-in)
            var overdueCheckIns = await context.Bookings
                .Where(b => b.Status == "Confirmed" && b.StartDate < today)
                .ToListAsync(cancellationToken);

            var allBookingsToCheckIn = bookingsToCheckIn.Concat(overdueCheckIns).ToList();

            if (!allBookingsToCheckIn.Any())
            {
                _logger.LogInformation("[BookingStatusUpdate] No bookings ready for check-in (Confirmed → InStay).");
                return;
            }

            _logger.LogInformation($"[BookingStatusUpdate] Found {allBookingsToCheckIn.Count} booking(s) to check in.");

            var updatedCount = 0;
            foreach (var booking in allBookingsToCheckIn)
            {
                try
                {
                    _logger.LogInformation(
                        $"[BookingStatusUpdate] Checking in booking #{booking.BookingId} " +
                        $"(StartDate: {booking.StartDate}, Current: {now:yyyy-MM-dd HH:mm})");

                    booking.Status = "InStay";
                    updatedCount++;

                    _logger.LogInformation($"[BookingStatusUpdate] Successfully updated booking #{booking.BookingId} to InStay");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[BookingStatusUpdate] Failed to update booking #{booking.BookingId} to InStay");
                }
            }

            if (updatedCount > 0)
            {
                try
                {
                    var savedChanges = await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        $"[BookingStatusUpdate] Check-in update completed. " +
                        $"Updated: {updatedCount}, SaveChanges rows: {savedChanges}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BookingStatusUpdate] Failed to save check-in changes to database");
                    throw;
                }
            }
        }

        /// <summary>
        /// Chuyển trạng thái InStay → Completed sau 12:00 ngày check-out
        /// Tự động tạo voucher và gửi email thông báo
        /// </summary>
        private async Task UpdateInStayToCompletedAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[BookingStatusUpdate] Checking for bookings to move from InStay to Completed...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CondotelDbVer1Context>();

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            // Tìm booking: Status = InStay, EndDate = today, và đã qua 12:00
            var bookingsToCheckOut = await context.Bookings
                .Where(b => b.Status == "InStay"
                         && b.EndDate == today
                         && currentTime >= new TimeOnly(12, 0)) // Đã qua 12:00
                .ToListAsync(cancellationToken);

            // Hoặc những booking có EndDate < today nhưng vẫn InStay (quá hạn check-out)
            var overdueCheckOuts = await context.Bookings
                .Where(b => b.Status == "InStay" && b.EndDate < today)
                .ToListAsync(cancellationToken);

            var allBookingsToCheckOut = bookingsToCheckOut.Concat(overdueCheckOuts).ToList();

            if (!allBookingsToCheckOut.Any())
            {
                _logger.LogInformation("[BookingStatusUpdate] No bookings ready for check-out (InStay → Completed).");
                return;
            }

            _logger.LogInformation($"[BookingStatusUpdate] Found {allBookingsToCheckOut.Count} booking(s) to check out.");

            var updatedCount = 0;
            var voucherService = scope.ServiceProvider.GetRequiredService<IVoucherService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            foreach (var booking in allBookingsToCheckOut)
            {
                try
                {
                    _logger.LogInformation(
                        $"[BookingStatusUpdate] Checking out booking #{booking.BookingId} " +
                        $"(EndDate: {booking.EndDate}, Current: {now:yyyy-MM-dd HH:mm})");

                    booking.Status = "Completed";

                    // Tự động tạo voucher nếu host có AutoGenerate = true
                    try
                    {
                        var vouchers = await voucherService.CreateVoucherAfterBookingAsync(booking.BookingId);

                        if (vouchers != null && vouchers.Any())
                        {
                            _logger.LogInformation($"[BookingStatusUpdate] Created {vouchers.Count} voucher(s) for booking #{booking.BookingId}");

                            // Gửi email thông báo voucher cho customer
                            var customer = await context.Users.FindAsync(booking.CustomerId);
                            if (customer != null && !string.IsNullOrEmpty(customer.Email))
                            {
                                try
                                {
                                    var voucherInfos = vouchers.Select(v => new CondotelManagement.Services.Interfaces.Shared.VoucherInfo
                                    {
                                        Code = v.Code,
                                        CondotelName = v.CondotelName ?? "N/A",
                                        DiscountAmount = v.DiscountAmount,
                                        DiscountPercentage = v.DiscountPercentage,
                                        StartDate = v.StartDate,
                                        EndDate = v.EndDate
                                    }).ToList();

                                    await emailService.SendVoucherNotificationEmailAsync(
                                        customer.Email,
                                        customer.FullName ?? "Khách hàng",
                                        booking.BookingId,
                                        voucherInfos
                                    );

                                    _logger.LogInformation($"[BookingStatusUpdate] Sent voucher notification email to {customer.Email} for booking #{booking.BookingId}");
                                }
                                catch (Exception emailEx)
                                {
                                    _logger.LogError(emailEx, $"[BookingStatusUpdate] Failed to send voucher email for booking #{booking.BookingId}");
                                }
                            }
                            else
                            {
                                _logger.LogWarning($"[BookingStatusUpdate] Customer email not found for booking #{booking.BookingId}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"[BookingStatusUpdate] No vouchers created for booking #{booking.BookingId}");
                        }
                    }
                    catch (Exception voucherEx)
                    {
                        _logger.LogError(voucherEx, $"[BookingStatusUpdate] Failed to create vouchers for booking #{booking.BookingId}");
                    }

                    updatedCount++;
                    _logger.LogInformation($"[BookingStatusUpdate] Successfully updated booking #{booking.BookingId} to Completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[BookingStatusUpdate] Failed to update booking #{booking.BookingId} to Completed");
                }
            }

            if (updatedCount > 0)
            {
                try
                {
                    var savedChanges = await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        $"[BookingStatusUpdate] Check-out update completed. " +
                        $"Updated: {updatedCount}, SaveChanges rows: {savedChanges}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BookingStatusUpdate] Failed to save check-out changes to database");
                    throw;
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 BookingStatusUpdateService is stopping gracefully...");
            await base.StopAsync(cancellationToken);
        }
    }
}