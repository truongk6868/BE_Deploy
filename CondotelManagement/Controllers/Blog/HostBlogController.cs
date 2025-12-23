using CondotelManagement.DTOs.Blog;
using CondotelManagement.Services.Interfaces.Auth;
using CondotelManagement.Services.Interfaces.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Nhớ thêm dòng này để dùng FirstOrDefaultAsync
using CondotelManagement.Data;
using System.Security.Claims;

namespace CondotelManagement.Controllers.Host
{
    [Route("api/host/blog")]
    [ApiController]
    [Authorize(Roles = "Host")] // Chỉ Host mới được truy cập
    public class HostBlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IAuthService _authService;
        private readonly CondotelDbVer1Context _context;// Hoặc dùng User.Claims trực tiếp

        public HostBlogController(IBlogService blogService, IAuthService authService, CondotelDbVer1Context context)
        {
            _blogService = blogService;
            _authService = authService;
            _context = context;
        }

        // API: Gửi yêu cầu đăng bài
        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest([FromBody] HostBlogRequestDto dto)
        {
            // 1. Lấy UserId từ Token
            // Cách 1: Dùng AuthService nếu bạn có hàm GetCurrentUser
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();
            int userId = user.UserId;

            // Cách 2 (Nhanh): Lấy từ Claims nếu AuthService chưa hỗ trợ
            // var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            // int userId = int.Parse(userIdClaim);

            // 2. Gọi Service xử lý
            var result = await _blogService.CreateHostBlogRequestAsync(userId, dto);

            // 3. Trả về kết quả dựa trên logic
            if (!result.Success)
            {
                // Nếu bị chặn do gói cước hoặc hết lượt -> Trả về 403 Forbidden hoặc 400 Bad Request
                if (result.Message.Contains("không hỗ trợ"))
                    return StatusCode(403, new { message = result.Message, package = result.CurrentPackage });

                return BadRequest(new { message = result.Message, remaining = result.RemainingQuota });
            }

            // Thành công -> 200 OK
            return Ok(result);
        }
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            // 1. Lấy User từ Token
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            // 2. Tìm HostId dựa trên UserId (Đây là bước bạn còn thiếu)
            var host = await _context.Hosts.FirstOrDefaultAsync(h => h.UserId == user.UserId);

            // Nếu User có Role Host nhưng chưa có dữ liệu trong bảng Hosts
            if (host == null)
            {
                return BadRequest(new { message = "Tài khoản chưa được kích hoạt thông tin Host." });
            }

            // 3. Truyền host.HostId vào Service (Thay vì user.UserId)
            var result = await _blogService.GetHostRequestsAsync(host.HostId);

            return Ok(result);
        }
        // Bạn có thể thêm API GetHistory để Host xem lại các bài mình đã gửi...
        [HttpPut("requests/{id}")]
        public async Task<IActionResult> UpdateRequest(int id, [FromBody] HostBlogRequestDto dto)
        {
            // 1. Lấy User từ Token
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            // 2. Tìm HostId tương tự như trên
            var host = await _context.Hosts.FirstOrDefaultAsync(h => h.UserId == user.UserId);

            if (host == null)
            {
                return BadRequest(new { message = "Không tìm thấy thông tin Host." });
            }

            // 3. Truyền host.HostId vào Service
            var result = await _blogService.ResubmitBlogRequestAsync(host.HostId, id, dto);

            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(result);
        }
        [HttpDelete("requests/{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            // 1. Lấy User từ Token
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            // 2. Tìm HostId
            var host = await _context.Hosts.FirstOrDefaultAsync(h => h.UserId == user.UserId);
            if (host == null) return BadRequest(new { message = "Không tìm thấy thông tin Host." });

            // 3. Gọi Service xóa
            var result = await _blogService.DeleteBlogRequestAsync(host.HostId, id);

            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }
        // GET: api/host/blog/requests/{id}
        [HttpGet("requests/{id}")]
        public async Task<IActionResult> GetRequestDetail(int id)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var host = await _context.Hosts
                .FirstOrDefaultAsync(h => h.UserId == user.UserId);

            if (host == null)
                return BadRequest(new { message = "Không tìm thấy thông tin Host." });

            var result = await _blogService.GetHostBlogRequestDetailAsync(host.HostId, id);

            if (!result.Success)
            {
                if (result.Message.Contains("không tìm thấy") || result.Message.Contains("quyền"))
                    return NotFound(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            // Thành công → trả về Data (là HostBlogRequestDto)
            return Ok(result.Data);
        }
    }
}