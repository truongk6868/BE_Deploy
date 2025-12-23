using CondotelManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CondotelManagement.Services.Background
{
    /// <summary>
    /// Background service tự động hủy các booking PENDING quá 10 phút chưa thanh toán
    /// Chạy mỗi phút để kiểm tra và hủy các booking quá hạn
    /// </summary>
    public class PendingBookingCancellationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PendingBookingCancellationService> _logger;
        private const int CancellationTimeoutMinutes = 10; // Hủy sau 10 phút
        private const int CheckIntervalSeconds = 60; // Kiểm tra mỗi 60 giây (1 phút)

        public PendingBookingCancellationService(
            IServiceProvider serviceProvider,
            ILogger<PendingBookingCancellationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PendingBookingCancellationService is starting. Will cancel PENDING bookings after {Minutes} minutes without payment.", 
                CancellationTimeoutMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelExpiredPendingBookingsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cancelling expired pending bookings.");
                }

                // Chờ 1 phút trước khi check lại
                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("PendingBookingCancellationService is stopping.");
        }

        /// <summary>
        /// Hủy các booking PENDING đã quá 10 phút chưa thanh toán
        /// </summary>
        private async Task CancelExpiredPendingBookingsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CondotelDbVer1Context>();

            try
            {
                // Tính thời điểm cutoff: bookings được tạo trước thời điểm này sẽ bị hủy
                var cutoffTime = DateTime.UtcNow.AddMinutes(-CancellationTimeoutMinutes);

                // Lấy các booking PENDING đã quá 10 phút
                var expiredBookings = await context.Bookings
                    .Where(b => b.Status == "Pending" 
                        && b.CreatedAt <= cutoffTime)
                    .ToListAsync();

                if (!expiredBookings.Any())
                {
                    _logger.LogDebug("No expired pending bookings found.");
                    return;
                }

                _logger.LogInformation("Found {Count} expired pending booking(s) to cancel.", expiredBookings.Count);

                int cancelledCount = 0;
                foreach (var booking in expiredBookings)
                {
                    try
                    {
                        // Chỉ hủy nếu vẫn còn là PENDING (tránh race condition)
                        if (booking.Status == "Pending")
                        {
                            booking.Status = "Cancelled";
                            _logger.LogInformation(
                                "Cancelled booking {BookingId} (Created: {CreatedAt}, Age: {AgeMinutes} minutes). Reason: Payment timeout after {TimeoutMinutes} minutes.",
                                booking.BookingId,
                                booking.CreatedAt,
                                (DateTime.UtcNow - booking.CreatedAt).TotalMinutes,
                                CancellationTimeoutMinutes);

                            cancelledCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error cancelling booking {BookingId}. Continuing with next booking.",
                            booking.BookingId);
                    }
                }

                if (cancelledCount > 0)
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation(
                        "Successfully cancelled {CancelledCount} expired pending booking(s).",
                        cancelledCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelExpiredPendingBookingsAsync.");
                throw;
            }
        }
    }
}



