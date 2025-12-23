using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CondotelManagement.Data;

namespace CondotelManagement.Services.BackgroundServices
{
    public class BookingTimeoutBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingTimeoutBackgroundService> _logger;
        private const int PAYMENT_TIMEOUT_MINUTES = 3;
        private const int CHECK_INTERVAL_SECONDS = 30; // Check mỗi 30 giây

        public BookingTimeoutBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<BookingTimeoutBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Timeout Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelExpiredPendingBookingsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Booking Timeout Background Service");
                }

                // Chờ 30 giây trước khi check lại
                await Task.Delay(TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS), stoppingToken);
            }

            _logger.LogInformation("Booking Timeout Background Service stopped");
        }

        private async Task CancelExpiredPendingBookingsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CondotelDbVer1Context>();

            // Tính thời điểm timeout (3 phút trước hiện tại)
            var timeoutThreshold = DateTime.Now.AddMinutes(-PAYMENT_TIMEOUT_MINUTES);

            // Tìm tất cả booking Pending đã quá hạn
            var expiredBookings = await context.Bookings
                .Where(b => b.Status == "Pending" && b.CreatedAt < timeoutThreshold)
                .ToListAsync();

            if (expiredBookings.Any())
            {
                _logger.LogInformation(
                    "Found {Count} expired pending bookings to cancel",
                    expiredBookings.Count);

                foreach (var booking in expiredBookings)
                {
                    booking.Status = "Cancelled";
                    _logger.LogInformation(
                        "Cancelled booking {BookingId} - Created at {CreatedAt}, Timeout at {TimeoutThreshold}",
                        booking.BookingId,
                        booking.CreatedAt,
                        timeoutThreshold);
                }

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully cancelled {Count} expired bookings",
                    expiredBookings.Count);
            }
        }
    }
}