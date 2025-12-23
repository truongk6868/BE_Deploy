using CondotelManagement.Data;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Controllers.Auth
{
    [ApiController]
    [Route("api/public-profile")]
    public class PublicProfileController : ControllerBase
    {
        private readonly CondotelDbVer1Context _context;
        private readonly IPackageService _packageService;

        public PublicProfileController(
            CondotelDbVer1Context context,
            IPackageService packageService)
        {
            _context = context;
            _packageService = packageService;
        }

        // GET: api/public-profile/host/{hostId}
        [HttpGet("host/{hostId}")]
        public async Task<IActionResult> GetHostPublicProfile(int hostId)
        {
            var host = await _context.Hosts
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.HostId == hostId);

            if (host == null || host.User == null)
                return NotFound(new { message = "Host không tồn tại" });

            var activePackage = await _packageService.GetMyActivePackageAsync(hostId);

            return Ok(new
            {
                HostId = hostId,
                FullName = host.User.FullName,
                ImageUrl = host.User.ImageUrl,
                Phone = host.PhoneContact,
                IsVerified = activePackage?.IsVerifiedBadgeEnabled ?? false,
                PackageName = activePackage?.PackageName,
                PriorityLevel = activePackage?.PriorityLevel ?? 0,
                DisplayColorTheme = activePackage?.DisplayColorTheme
            });
        }
    }
}
