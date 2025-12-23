using CondotelManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CondotelManagement.Services.Background
{
    /// <summary>
    /// Background service tự động cập nhật trạng thái promotion khi hết hạn
    /// Chạy mỗi ngày lúc 00:00 giờ Việt Nam (UTC+7) để cập nhật các promotion đã hết hạn
    /// </summary>
    public class PromotionStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PromotionStatusUpdateService> _logger;
        private const int BatchSize = 100; // Xử lý 100 promotions mỗi batch
        private readonly TimeZoneInfo _vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public PromotionStatusUpdateService(
            IServiceProvider serviceProvider,
            ILogger<PromotionStatusUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PromotionStatusUpdateService is starting.");

            // Chờ đến 00:00 giờ Việt Nam đầu tiên
            await WaitUntilMidnight(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTimeZone);
                    _logger.LogInformation("Running scheduled promotion status update at {Time} (Vietnam Time)", vietnamTime);
                    await UpdateExpiredPromotionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating promotion statuses.");
                }

                // Chờ đến 00:00 giờ Việt Nam ngày hôm sau
                await WaitUntilMidnight(stoppingToken);
            }

            _logger.LogInformation("PromotionStatusUpdateService is stopping.");
        }

        /// <summary>
        /// Chờ đến 00:00 giờ Việt Nam tiếp theo
        /// </summary>
        private async Task WaitUntilMidnight(CancellationToken stoppingToken)
        {
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTimeZone);
            var midnight = vietnamTime.Date.AddDays(1); // 00:00 ngày hôm sau (giờ Việt Nam)
            var delay = midnight - vietnamTime;

            _logger.LogInformation("Waiting until {Midnight} Vietnam Time ({Delay} from now)", midnight, delay);
            await Task.Delay(delay, stoppingToken);
        }

        /// <summary>
        /// Cập nhật status của các promotion đã hết hạn
        /// Xử lý theo batch để tối ưu performance
        /// </summary>
        private async Task UpdateExpiredPromotionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CondotelDbVer1Context>();

            try
            {
                // Lấy giờ Việt Nam (UTC+7)
                var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTimeZone);
                var today = DateOnly.FromDateTime(vietnamTime);
                int totalProcessed = 0;

                // Lấy tổng số promotions cần xử lý (đã hết hạn và đang Active)
                var totalCount = await context.Promotions
                    .Where(p => p.EndDate < today && p.Status == "Active")
                    .CountAsync();

                if (totalCount == 0)
                {
                    _logger.LogInformation("No expired promotions to update.");
                    return;
                }

                _logger.LogInformation("Found {Count} expired promotion(s) to update. Processing in batches of {BatchSize}.", 
                    totalCount, BatchSize);

                // Xử lý theo batch
                int skip = 0;
                while (skip < totalCount)
                {
                    // Lấy batch promotions
                    var promotionsBatch = await context.Promotions
                        .Where(p => p.EndDate < today && p.Status == "Active")
                        .OrderBy(p => p.PromotionId) // Đảm bảo thứ tự nhất quán
                        .Skip(skip)
                        .Take(BatchSize)
                        .ToListAsync();

                    if (!promotionsBatch.Any())
                        break;

                    _logger.LogInformation("Processing batch: {Current}/{Total} promotions (Batch {BatchNumber})", 
                        skip + promotionsBatch.Count, totalCount, (skip / BatchSize) + 1);

                    // Cập nhật status cho từng promotion trong batch
                    foreach (var promotion in promotionsBatch)
                    {
                        try
                        {
                            // Cập nhật status thành "Inactive" hoặc "Expired"
                            promotion.Status = "Inactive";
                            _logger.LogDebug(
                                "Updated promotion {PromotionId} ({Name}) status from Active to Inactive. EndDate: {EndDate}",
                                promotion.PromotionId, promotion.Name, promotion.EndDate);

                            totalProcessed++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Error processing promotion {PromotionId}. Continuing with next promotion.",
                                promotion.PromotionId);
                            // Tiếp tục với promotion tiếp theo
                        }
                    }

                    // Lưu thay đổi cho batch này
                    await context.SaveChangesAsync();

                    skip += BatchSize;
                }

                _logger.LogInformation(
                    "Completed processing: {Processed}/{Total} promotions updated to Inactive.",
                    totalProcessed, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promotion statuses.");
                throw;
            }
        }
    }
}



