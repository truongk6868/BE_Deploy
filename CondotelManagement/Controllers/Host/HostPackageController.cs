using CondotelManagement.Data;
using CondotelManagement.DTOs.Package;
using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CondotelManagement.Controllers.Host
{
    [Route("api/host/packages")]
    [ApiController]
    [Authorize] // Chỉ cần đăng nhập
    public class HostPackageController : ControllerBase
    {
        private readonly IPackageService _packageService;
        private readonly CondotelDbVer1Context _context;

        public HostPackageController(IPackageService packageService, CondotelDbVer1Context context)
        {
            _packageService = packageService;
            _context = context;
        }

        private async Task<int> GetOrCreateHostIdAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var host = await _context.Hosts
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (host == null)
            {
                host = new Models.Host
                {
                    UserId = userId,
                    PhoneContact = "",
                    Address = null
                    // Chỉ set những field thực sự có trong bảng Host của bạn
                };
                _context.Hosts.Add(host);
                await _context.SaveChangesAsync();

                // Role "Host" sẽ được gán ở nơi khác (ví dụ: webhook PayOS thành công)
                // Hoặc bạn có thể gán thủ công trong DB sau
            }

            return host.HostId;
        }

        [HttpGet("my-package")]
        public async Task<IActionResult> GetMyPackage()
        {
            try
            {
                var hostId = await GetOrCreateHostIdAsync();
                var package = await _packageService.GetMyActivePackageAsync(hostId);
                return Ok(package ?? null);
            }
            catch
            {
                return Ok(null);
            }
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> PurchasePackage([FromBody] PurchasePackageRequestDto request)
        {
            if (request == null || request.PackageId <= 0)
                return BadRequest(new { message = "PackageId không hợp lệ." });

            try
            {
                var hostId = await GetOrCreateHostIdAsync();
                var result = await _packageService.PurchaseOrUpgradePackageAsync(hostId, request.PackageId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST api/host/packages/cancel - ĐÃ VÔ HIỆU HÓA
        // Host không thể hủy package, chỉ có thể nâng cấp gói
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelPackage([FromBody] CancelPackageRequestDTO? request)
        {
            return BadRequest(new { 
                message = "Host không thể hủy package. Bạn chỉ có thể nâng cấp lên gói cao hơn bằng cách mua gói mới.",
                canUpgrade = true 
            });
        }
    }

    public class PurchasePackageRequestDto
    {
        public int PackageId { get; set; }
    }
}