// File: Controllers/Admin/AdminPackageController.cs
using CondotelManagement.Data;
using CondotelManagement.DTOs.Admin;
using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/packages")]
    [Authorize(Roles = "Admin")]
    public class AdminPackageController : ControllerBase
    {
        private readonly CondotelDbVer1Context _context;
        private readonly IPackageFeatureService _featureService;

        public AdminPackageController(CondotelDbVer1Context context, IPackageFeatureService featureService)
        {
            _context = context;
            _featureService = featureService;
        }

        // ==========================================
        // PHẦN 1: QUẢN LÝ ĐƠN HÀNG (HostPackages)
        // ==========================================

        // GET: api/admin/packages?search=abc
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search = null)
        {
            try
            {
                var query = _context.HostPackages
                    .Include(hp => hp.Host!)
                        .ThenInclude(h => h.User!)
                    .Include(hp => hp.Package!)
                    .AsQueryable();

                // Sắp xếp trước khi Where để tránh lỗi OrderedQueryable
                query = query.OrderByDescending(hp => hp.HostPackageId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(hp =>
                        (hp.OrderCode != null && hp.OrderCode.ToLower().Contains(search)) ||
                        (hp.Host != null && hp.Host.User != null && (
                            (hp.Host.User.Email != null && hp.Host.User.Email.ToLower().Contains(search)) ||
                            (hp.Host.User.Phone != null && hp.Host.User.Phone.Contains(search)) ||
                            (hp.Host.User.FullName != null && hp.Host.User.FullName.ToLower().Contains(search))
                        ))
                    );
                }

                var result = await query.Select(hp => new
                {
                    hp.HostPackageId,
                    HostName = hp.Host != null && hp.Host.User != null ?
                        (hp.Host.User.FullName ?? "Chưa có tên") : "Không xác định",
                    Email = hp.Host != null && hp.Host.User != null ?
                        (hp.Host.User.Email ?? "-") : "-",
                    Phone = hp.Host != null && hp.Host.User != null ?
                        (hp.Host.User.Phone ?? "-") : "-",
                    PackageName = hp.Package != null ? hp.Package.Name : "Không xác định",
                    hp.OrderCode,
                    Amount = hp.Package != null ? (hp.Package.Price ?? 0) : 0,
                    hp.Status,
                    StartDate = hp.StartDate.HasValue ? hp.StartDate.Value.ToString("dd/MM/yyyy") : "-",
                    EndDate = hp.EndDate.HasValue ? hp.EndDate.Value.ToString("dd/MM/yyyy") : "-",
                    CanActivate = hp.Status == "PendingPayment"
                }).ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Debug logging
                Console.WriteLine($"ERROR in GetAll: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    error = "Database error",
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        // POST: api/admin/packages/123/activate
        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ManualActivate(int id)
        {
            try
            {
                var hp = await _context.HostPackages
                    .Include(h => h.Host).ThenInclude(h => h!.User)
                    .FirstOrDefaultAsync(h => h.HostPackageId == id);

                if (hp == null) return NotFound("Không tìm thấy đơn hàng");

                if (hp.Status == "Active")
                    return BadRequest("Gói này đã được kích hoạt rồi!");

                var today = DateOnly.FromDateTime(DateTime.Today);
                var duration = hp.DurationDays ?? 30;

                hp.Status = "Active";
                hp.StartDate = today;
                hp.EndDate = today.AddDays(duration);

                // Nâng role Host lên 4 (nếu chưa phải)
                if (hp.Host?.User != null && hp.Host.User.RoleId != 4)
                {
                    hp.Host.User.RoleId = 4;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Đã kích hoạt gói thành công cho Host!",
                    hostName = hp.Host?.User?.FullName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================
        // PHẦN 2: CRUD DANH MỤC GÓI (Packages - Catalog)
        // ==========================================

        // 1. LẤY DANH SÁCH GÓI - XÓA PHƯƠNG THỨC CŨ, CHỈ GIỮ PHƯƠNG THỨC MỚI
        [HttpGet("catalog")]
        public async Task<ActionResult<List<AdminPackageDtos.CatalogPackageDto>>> GetAllCatalogPackages()
        {
            var packages = await _context.Packages.ToListAsync();

            var result = packages.Select(p => new AdminPackageDtos.CatalogPackageDto
            {
                PackageId = p.PackageId,
                Name = p.Name ?? "Chưa đặt tên",
                Price = p.Price ?? 0,
                DurationDays = int.TryParse(p.Duration, out int d) ? d : null,
                Description = p.Description,
                IsActive = p.Status == "Active",

                // THÊM CÁC TRƯỜNG FEATURES
                MaxListingCount = p.MaxListingCount ?? 0,
                CanUseFeaturedListing = p.CanUseFeaturedListing ?? false,
                MaxBlogRequestsPerMonth = p.MaxBlogRequestsPerMonth ?? 0,
                IsVerifiedBadgeEnabled = p.IsVerifiedBadgeEnabled ?? false,
                DisplayColorTheme = p.DisplayColorTheme ?? "default",
                PriorityLevel = p.PriorityLevel ?? 0
            }).ToList();

            return Ok(result);
        }

        // 2. LẤY CHI TIẾT 1 GÓI - ĐÃ SỬA
        [HttpGet("catalog/{id}")]
        public async Task<ActionResult<AdminPackageDtos.CatalogPackageDto>> GetCatalogPackageById(int id)
        {
            var p = await _context.Packages.FindAsync(id);
            if (p == null) return NotFound("Không tìm thấy gói dịch vụ");

            var result = new AdminPackageDtos.CatalogPackageDto
            {
                PackageId = p.PackageId,
                Name = p.Name ?? "Chưa đặt tên",
                Price = p.Price ?? 0,
                DurationDays = int.TryParse(p.Duration, out int d) ? d : null,
                Description = p.Description,
                IsActive = p.Status == "Active",

                // THÊM CÁC TRƯỜNG FEATURES
                MaxListingCount = p.MaxListingCount ?? 0,
                CanUseFeaturedListing = p.CanUseFeaturedListing ?? false,
                MaxBlogRequestsPerMonth = p.MaxBlogRequestsPerMonth ?? 0,
                IsVerifiedBadgeEnabled = p.IsVerifiedBadgeEnabled ?? false,
                DisplayColorTheme = p.DisplayColorTheme ?? "default",
                PriorityLevel = p.PriorityLevel ?? 0
            };

            return Ok(result);
        }

        // 3. TẠO GÓI MỚI - ĐÃ SỬA
        [HttpPost("catalog")]
        public async Task<IActionResult> CreateCatalogPackage([FromBody] AdminPackageDtos.PackageCreateDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newPackage = new Package
            {
                Name = request.Name,
                Price = request.Price ?? 0,
                Duration = request.DurationDays?.ToString(),
                Description = request.Description,
                Status = "Active",

                // THÊM CÁC TRƯỜNG FEATURES
                MaxListingCount = request.MaxListingCount,
                CanUseFeaturedListing = request.CanUseFeaturedListing,
                MaxBlogRequestsPerMonth = request.MaxBlogRequestsPerMonth,
                IsVerifiedBadgeEnabled = request.IsVerifiedBadgeEnabled,
                DisplayColorTheme = request.DisplayColorTheme,
                PriorityLevel = request.PriorityLevel
            };

            _context.Packages.Add(newPackage);
            await _context.SaveChangesAsync();

            var response = new AdminPackageDtos.CatalogPackageDto
            {
                PackageId = newPackage.PackageId,
                Name = newPackage.Name,
                Price = newPackage.Price ?? 0,
                DurationDays = request.DurationDays,
                Description = newPackage.Description,
                IsActive = true,

                // THÊM CÁC TRƯỜNG FEATURES VÀO RESPONSE
                MaxListingCount = newPackage.MaxListingCount ?? 0,
                CanUseFeaturedListing = newPackage.CanUseFeaturedListing ?? false,
                MaxBlogRequestsPerMonth = newPackage.MaxBlogRequestsPerMonth ?? 0,
                IsVerifiedBadgeEnabled = newPackage.IsVerifiedBadgeEnabled ?? false,
                DisplayColorTheme = newPackage.DisplayColorTheme ?? "default",
                PriorityLevel = newPackage.PriorityLevel ?? 0
            };

            return Ok(new { message = "Tạo gói mới thành công", data = response });
        }

        // 4. CẬP NHẬT GÓI - ĐÃ SỬA
        [HttpPut("catalog/{id}")]
        public async Task<IActionResult> UpdateCatalogPackage(int id, [FromBody] AdminPackageDtos.PackageUpdateDto request)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null) return NotFound("Không tìm thấy gói dịch vụ");

            // Cập nhật thông tin cơ bản
            package.Name = request.Name;
            package.Price = request.Price ?? 0;
            package.Duration = request.DurationDays?.ToString();
            package.Description = request.Description;
            package.Status = request.IsActive ? "Active" : "Inactive";

            // Cập nhật các trường features
            package.MaxListingCount = request.MaxListingCount;
            package.CanUseFeaturedListing = request.CanUseFeaturedListing;
            package.MaxBlogRequestsPerMonth = request.MaxBlogRequestsPerMonth;
            package.IsVerifiedBadgeEnabled = request.IsVerifiedBadgeEnabled;
            package.DisplayColorTheme = request.DisplayColorTheme;
            package.PriorityLevel = request.PriorityLevel;

            await _context.SaveChangesAsync();

            var response = new AdminPackageDtos.CatalogPackageDto
            {
                PackageId = package.PackageId,
                Name = package.Name,
                Price = package.Price ?? 0,
                DurationDays = request.DurationDays,
                Description = package.Description,
                IsActive = request.IsActive,

                // THÊM CÁC TRƯỜNG FEATURES VÀO RESPONSE
                MaxListingCount = package.MaxListingCount ?? 0,
                CanUseFeaturedListing = package.CanUseFeaturedListing ?? false,
                MaxBlogRequestsPerMonth = package.MaxBlogRequestsPerMonth ?? 0,
                IsVerifiedBadgeEnabled = package.IsVerifiedBadgeEnabled ?? false,
                DisplayColorTheme = package.DisplayColorTheme ?? "default",
                PriorityLevel = package.PriorityLevel ?? 0
            };

            return Ok(new { message = "Cập nhật thành công", data = response });
        }

        // 5. XÓA GÓI - GIỮ NGUYÊN
        [HttpDelete("catalog/{id}")]
        public async Task<IActionResult> DeleteCatalogPackage(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null) return NotFound(new { message = "Không tìm thấy gói dịch vụ" });

            var isUsed = await _context.HostPackages.AnyAsync(hp => hp.PackageId == id);
            if (isUsed)
                return BadRequest(new { message = "Gói này đã có người mua, không thể xóa! Hãy chuyển trạng thái sang ngưng hoạt động." });

            _context.Packages.Remove(package);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa gói dịch vụ thành công" });
        }
    }
}