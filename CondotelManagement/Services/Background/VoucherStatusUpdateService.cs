using CondotelManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CondotelManagement.Services.Background
{
    /// <summary>
    /// Background service tự động cập nhật trạng thái voucher khi hết hạn
    /// Chạy mỗi ngày lúc 00:00 UTC để cập nhật các voucher đã hết hạn
    /// </summary>
    public class VoucherStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VoucherStatusUpdateService> _logger;
        private const int BatchSize = 100; // Xử lý 100 vouchers mỗi batch

        public VoucherStatusUpdateService(
            IServiceProvider serviceProvider,
            ILogger<VoucherStatusUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VoucherStatusUpdateService is starting.");

            // Chờ đến 00:00 UTC đầu tiên
            await WaitUntilMidnight(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running scheduled voucher status update at {Time}", DateTime.UtcNow);
                    await UpdateExpiredVouchersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating voucher statuses.");
                }

                // Chờ đến 00:00 UTC ngày hôm sau
                await WaitUntilMidnight(stoppingToken);
            }

            _logger.LogInformation("VoucherStatusUpdateService is stopping.");
        }

        /// <summary>
        /// Chờ đến 00:00 UTC tiếp theo
        /// </summary>
        private async Task WaitUntilMidnight(CancellationToken stoppingToken)
        {
            var now = DateTime.UtcNow;
            var midnight = now.Date.AddDays(1); // 00:00 ngày hôm sau
            var delay = midnight - now;

            _logger.LogInformation("Waiting until {Midnight} UTC ({Delay} from now)", midnight, delay);
            await Task.Delay(delay, stoppingToken);
        }

        /// <summary>
        /// Cập nhật status của các voucher đã hết hạn
        /// Xử lý theo batch để tối ưu performance
        /// </summary>
        private async Task UpdateExpiredVouchersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CondotelDbVer1Context>();

            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                int totalProcessed = 0;

                // Lấy tổng số vouchers cần xử lý (đã hết hạn và đang Active)
                var totalCount = await context.Vouchers
                    .Where(v => v.EndDate < today && (v.Status == "Active" || v.Status == null))
                    .CountAsync();

                if (totalCount == 0)
                {
                    _logger.LogInformation("No expired vouchers to update.");
                    return;
                }

                _logger.LogInformation("Found {Count} expired voucher(s) to update. Processing in batches of {BatchSize}.", 
                    totalCount, BatchSize);

                // Xử lý theo batch
                int skip = 0;
                while (skip < totalCount)
                {
                    // Lấy batch vouchers
                    var vouchersBatch = await context.Vouchers
                        .Where(v => v.EndDate < today && (v.Status == "Active" || v.Status == null))
                        .OrderBy(v => v.VoucherId) // Đảm bảo thứ tự nhất quán
                        .Skip(skip)
                        .Take(BatchSize)
                        .ToListAsync();

                    if (!vouchersBatch.Any())
                        break;

                    _logger.LogInformation("Processing batch: {Current}/{Total} vouchers (Batch {BatchNumber})", 
                        skip + vouchersBatch.Count, totalCount, (skip / BatchSize) + 1);

                    // Cập nhật status cho từng voucher trong batch
                    foreach (var voucher in vouchersBatch)
                    {
                        try
                        {
                            // Lưu status cũ trước khi thay đổi
                            var oldStatus = voucher.Status ?? "null";
                            
                            // Cập nhật status thành "Expired"
                            voucher.Status = "Expired";
                            _logger.LogDebug(
                                "Updated voucher {VoucherId} ({Code}) status from {OldStatus} to Expired. EndDate: {EndDate}",
                                voucher.VoucherId, voucher.Code, oldStatus, voucher.EndDate);

                            totalProcessed++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Error processing voucher {VoucherId}. Continuing with next voucher.",
                                voucher.VoucherId);
                            // Tiếp tục với voucher tiếp theo
                        }
                    }

                    // Lưu thay đổi cho batch này
                    await context.SaveChangesAsync();

                    skip += BatchSize;
                }

                _logger.LogInformation(
                    "Completed processing: {Processed}/{Total} vouchers updated to Expired.",
                    totalProcessed, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating voucher statuses.");
                throw;
            }
        }
    }
}

