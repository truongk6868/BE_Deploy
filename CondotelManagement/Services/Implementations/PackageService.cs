using CondotelManagement.Data;
using CondotelManagement.DTOs.Package;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Admin;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Services.Interfaces.Auth;
using CondotelManagement.Services.Interfaces.Payment;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CondotelManagement.Services.Implementations
{
    public class PackageService : IPackageService
    {
        private readonly CondotelDbVer1Context _context;
        private readonly IPackageFeatureService _featureService;
        private readonly IUserRepository _userRepo;
        private readonly IPayOSService _payOSService;

        public PackageService(CondotelDbVer1Context context, IPackageFeatureService featureService, IUserRepository userRepo, IPayOSService payOSService)
        {
            _context = context;
            _featureService = featureService;
            _userRepo = userRepo;
            _payOSService = payOSService;
        }

        public async Task<IEnumerable<PackageDto>> GetAvailablePackagesAsync()
        {
            var packages = await _context.Packages
                .Where(p => p.Status == "Active")
                .ToListAsync();

            return packages.Select(p => new PackageDto
            {
                PackageId = p.PackageId,
                Name = p.Name,
                Price = p.Price.GetValueOrDefault(0),
                Duration = p.Duration ?? "30 days",
                Description = p.Description ?? string.Empty,
                MaxListings = _featureService.GetMaxListingCount(p.PackageId),
                CanUseFeaturedListing = _featureService.CanUseFeaturedListing(p.PackageId),
                MaxBlogRequestsPerMonth = _featureService.GetMaxBlogRequestsPerMonth(p.PackageId),
                IsVerifiedBadgeEnabled = _featureService.IsVerifiedBadgeEnabled(p.PackageId),
                DisplayColorTheme = _featureService.GetDisplayColorTheme(p.PackageId),
                PriorityLevel = _featureService.GetPriorityLevel(p.PackageId)
            });
        }

        public async Task<HostPackageDetailsDto?> GetMyActivePackageAsync(int hostId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Get the newest package for the host (prioritize Active, then PendingPayment)
            // Do not filter EndDate so pending packages are included
            var hostPackage = await _context.HostPackages
                .Include(hp => hp.Package)
                .Where(hp => hp.HostId == hostId)
                .OrderByDescending(hp => hp.Status == "Active" ? 1 : 0)
                .ThenByDescending(hp => hp.StartDate ?? DateOnly.MinValue)
                .FirstOrDefaultAsync();

            if (hostPackage == null) return null;

            // If the package is Active but expired, still return it for display
            if (hostPackage.Status == "Active" && hostPackage.EndDate.HasValue && hostPackage.EndDate < today)
            {
                // Package đã hết hạn nhưng vẫn trả về để hiển thị
            }

            var currentListings = await _context.Condotels
                .CountAsync(c => c.HostId == hostId && c.Status != "Deleted");

            // Count blog requests in the current month
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var usedBlogRequests = await _context.BlogRequests
                .CountAsync(br => br.HostId == hostId && br.RequestDate >= startOfMonth);

            return MapToDetailsDto(hostPackage, currentListings, usedBlogRequests);
        }

        public async Task<HostPackageDetailsDto> PurchaseOrUpgradePackageAsync(int hostId, int packageId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // ---------------------------------------------------------
            // 1. LẤY THÔNG TIN GÓI KHÁCH MUỐN MUA
            // ---------------------------------------------------------
            var packageToBuy = await _context.Packages
                .FirstOrDefaultAsync(p => p.PackageId == packageId && p.Status == "Active");

            if (packageToBuy == null)
                throw new Exception("Gói dịch vụ không hợp lệ hoặc đã ngừng hoạt động.");

            // ---------------------------------------------------------
            // 2. VALIDATION: KIỂM TRA GÓI ĐANG ACTIVE CỦA HOST
            // ---------------------------------------------------------
            // Tìm gói đang Active và chưa hết hạn của Host này
            var currentActivePackage = await _context.HostPackages
                .Include(hp => hp.Package)
                .Where(hp => hp.HostId == hostId
                             && hp.Status == "Active"
                             && (hp.EndDate == null || hp.EndDate >= today))
                .OrderByDescending(hp => hp.Package.Price) // Lấy gói xịn nhất nếu có nhiều
                .FirstOrDefaultAsync();

            if (currentActivePackage != null)
            {
                // CASE A: Khách mua đúng gói đang dùng -> CHẶN
                if (currentActivePackage.PackageId == packageId)
                {
                    throw new Exception($"Bạn đang sử dụng gói {currentActivePackage.Package.Name} (Hết hạn: {currentActivePackage.EndDate}). Bạn không thể mua lại khi gói chưa hết hạn.");
                }

                // CASE B: Khách mua gói rẻ hơn hoặc bằng tiền -> CHẶN (Chỉ cho nâng cấp)
                if (packageToBuy.Price <= currentActivePackage.Package.Price)
                {
                    throw new Exception($"Bạn đang dùng gói {currentActivePackage.Package.Name}. Bạn chỉ có thể nâng cấp lên gói cao cấp hơn.");
                }
            }

            // ---------------------------------------------------------
            // [ĐÃ XÓA] BƯỚC 3: KHÔNG ĐƯỢC SET INACTIVE GÓI CŨ TẠI ĐÂY
            // ---------------------------------------------------------

            // ---------------------------------------------------------
            // 4. TẠO MÃ ĐƠN HÀNG (ORDER CODE)
            // ---------------------------------------------------------
            var randomPart = new Random().Next(100000, 999999);
            var orderCode = (long)hostId * 1_000_000_000L + (long)packageId * 1_000_000L + randomPart;

            // Đảm bảo OrderCode không quá lớn (giới hạn của long/PayOS)
            while (orderCode > 99999999999999L)
            {
                randomPart = new Random().Next(100000, 999999);
                orderCode = (long)hostId * 1_000_000_000L + (long)packageId * 1_000_000L + randomPart;
            }

            // Giả sử hàm ParseDuration đã có sẵn trong class của bạn
            var durationDays = ParseDuration(packageToBuy.Duration ?? "30 days");

            // ---------------------------------------------------------
            // 5. UPSERT (UPDATE HOẶC INSERT) VỚI TRẠNG THÁI "PendingPayment"
            // ---------------------------------------------------------
            // Tìm xem trong DB đã từng có dòng {HostId, PackageId} này chưa
            var existingHostPackage = await _context.HostPackages
                .FirstOrDefaultAsync(hp => hp.HostId == hostId && hp.PackageId == packageId);

            if (existingHostPackage != null)
            {
                // === UPDATE: Đã từng mua gói này (có thể là Cancelled hoặc Expired) ===
                // Reset lại trạng thái để chờ thanh toán
                existingHostPackage.Status = "PendingPayment";
                existingHostPackage.OrderCode = orderCode.ToString();
                existingHostPackage.DurationDays = durationDays;
                existingHostPackage.StartDate = null; // Chưa thanh toán chưa tính ngày
                existingHostPackage.EndDate = null;
            }
            else
            {
                // === INSERT: Gói mới hoàn toàn ===
                var newHostPackage = new HostPackage
                {
                    HostId = hostId,
                    PackageId = packageId,
                    Status = "PendingPayment", // Quan trọng: Chỉ là PendingPayment
                    DurationDays = durationDays,
                    OrderCode = orderCode.ToString(),
                    StartDate = null,
                    EndDate = null
                };
                _context.HostPackages.Add(newHostPackage);
            }

            // Lưu tất cả thay đổi
            await _context.SaveChangesAsync();

            // ---------------------------------------------------------
            // 6. TRẢ VỀ DTO
            // ---------------------------------------------------------
            var currentListings = await _context.Condotels
                .CountAsync(c => c.HostId == hostId && c.Status != "Deleted");

            return new HostPackageDetailsDto
            {
                PackageName = packageToBuy.Name,
                Status = "PendingPayment",
                StartDate = null,
                EndDate = null,
                CurrentListings = currentListings,
                // Các thông tin features lấy từ Service
                MaxListings = _featureService.GetMaxListingCount(packageId),
                CanUseFeaturedListing = _featureService.CanUseFeaturedListing(packageId),
                MaxBlogRequestsPerMonth = _featureService.GetMaxBlogRequestsPerMonth(packageId),
                UsedBlogRequestsThisMonth = 0,
                IsVerifiedBadgeEnabled = _featureService.IsVerifiedBadgeEnabled(packageId),
                DisplayColorTheme = _featureService.GetDisplayColorTheme(packageId),
                PriorityLevel = _featureService.GetPriorityLevel(packageId),

                Message = "Đã tạo đơn hàng! Đang chuyển hướng thanh toán...",
                PaymentUrl = null, // Controller sẽ điền link sau
                OrderCode = orderCode,
                Amount = packageToBuy.Price.GetValueOrDefault(0)
            };
        }

        private int ParseDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration)) return 30;
            var match = Regex.Match(duration, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int days))
            {
                return days;
            }
            return 30;
        }

        private HostPackageDetailsDto MapToDetailsDto(HostPackage hostPackage, int currentListings, int usedBlogRequests)
        {
            // Extract OrderCode and Amount from package
            long orderCode = 0;
            if (!string.IsNullOrEmpty(hostPackage.OrderCode) && long.TryParse(hostPackage.OrderCode, out long parsedOrderCode))
            {
                orderCode = parsedOrderCode;
            }

            decimal amount = hostPackage.Package?.Price ?? 0;

            return new HostPackageDetailsDto
            {
                PackageName = hostPackage.Package?.Name ?? "Unknown",
                Status = hostPackage.Status,
                StartDate = hostPackage.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                EndDate = hostPackage.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                CurrentListings = currentListings,
                MaxListings = _featureService.GetMaxListingCount(hostPackage.PackageId),
                CanUseFeaturedListing = _featureService.CanUseFeaturedListing(hostPackage.PackageId),
                MaxBlogRequestsPerMonth = _featureService.GetMaxBlogRequestsPerMonth(hostPackage.PackageId),
                UsedBlogRequestsThisMonth = usedBlogRequests,
                IsVerifiedBadgeEnabled = _featureService.IsVerifiedBadgeEnabled(hostPackage.PackageId),
                DisplayColorTheme = _featureService.GetDisplayColorTheme(hostPackage.PackageId),
                PriorityLevel = _featureService.GetPriorityLevel(hostPackage.PackageId),
                Message = hostPackage.Status == "PendingPayment" ? "Pending payment" : null,
                PaymentUrl = null,
                OrderCode = orderCode,
                Amount = amount
            };
        }

        public async Task<CancelPackageResponseDTO> CancelPackageAsync(int hostId, CancelPackageRequestDTO request)
        {
            // Get the active package for the host
            var hostPackage = await _context.HostPackages
                .Include(hp => hp.Package)
                .Include(hp => hp.Host)
                    .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(hp => hp.HostId == hostId && hp.Status == "Active");

            if (hostPackage == null)
            {
                return new CancelPackageResponseDTO
                {
                    Success = false,
                    Message = "Không tìm thấy package đang active để hủy."
                };
            }

            // Check if the package has been paid (StartDate and EndDate set)
            if (!hostPackage.StartDate.HasValue || !hostPackage.EndDate.HasValue)
            {
                // Package not activated (not paid) - just cancel
                hostPackage.Status = "Cancelled";
                await _context.SaveChangesAsync();

                return new CancelPackageResponseDTO
                {
                    Success = true,
                    Message = "Package cancelled successfully (not paid)."
                };
            }

            // Package was paid - calculate refund
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var startDate = hostPackage.StartDate.Value;
            var endDate = hostPackage.EndDate.Value;
            var totalDays = (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days;
            var daysUsed = (today.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days;
            var daysRemaining = totalDays - daysUsed;

            // Calculate refund amount based on remaining days
            var packagePrice = hostPackage.Package?.Price ?? 0;
            decimal refundAmount = 0;

            if (daysRemaining > 0 && totalDays > 0)
            {
                // Pro-rate refund by remaining days
                refundAmount = (packagePrice * daysRemaining) / totalDays;
                refundAmount = Math.Round(refundAmount, 2);
            }

            // If refund >= 10,000 VND then create refund payment link
            string? refundPaymentLink = null;
            if (refundAmount >= 10000)
            {
                try
                {
                    // Get host info
                    var host = hostPackage.Host;
                    var user = host?.User;

                    // Create refund payment link via PayOS using package OrderCode
                    if (!string.IsNullOrEmpty(hostPackage.OrderCode) && long.TryParse(hostPackage.OrderCode, out long orderCode))
                    {
                        // Create payment link for host to receive refund
                        var refundResponse = await _payOSService.CreatePackageRefundPaymentLinkAsync(
                            hostId,
                            orderCode,
                            refundAmount,
                            user?.FullName ?? "Host",
                            user?.Email,
                            user?.Phone
                        );

                        if (refundResponse?.Data != null)
                        {
                            refundPaymentLink = refundResponse.Data.CheckoutUrl;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue cancelling
                    Console.WriteLine($"[CancelPackage] Error creating refund link: {ex.Message}");
                }
            }

            // Update status to Cancelled
            hostPackage.Status = "Cancelled";
            await _context.SaveChangesAsync();

            // Downgrade user role to Tenant if no other active packages
            var hasOtherActivePackage = await _context.HostPackages
                .AnyAsync(hp => hp.HostId == hostId && hp.Status == "Active" && hp.EndDate >= today);

            if (!hasOtherActivePackage && hostPackage.Host?.UserId != null)
            {
                var user = await _context.Users.FindAsync(hostPackage.Host.UserId);
                if (user != null && user.RoleId == 4) // RoleId 4 = Host
                {
                    user.RoleId = 3; // RoleId 3 = Tenant
                    await _context.SaveChangesAsync();
                }
            }

            return new CancelPackageResponseDTO
            {
                Success = true,
                Message = refundAmount >= 10000
                    ? "Package cancelled successfully. Refund link created."
                    : "Package cancelled successfully. Refund amount below minimum (10,000 VND).",
                RefundAmount = refundAmount >= 10000 ? refundAmount : null,
                RefundPaymentLink = refundPaymentLink,
                DaysUsed = daysUsed,
                DaysRemaining = daysRemaining > 0 ? daysRemaining : 0
            };
        }
    }
}
