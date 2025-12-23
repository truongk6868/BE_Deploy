using CondotelManagement.DTOs.Blog;
using CondotelManagement.Services.Interfaces.Blog; // Sửa Using
using CondotelManagement.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
    [Route("api/admin/blog")]
    [ApiController]
    [Authorize(Roles = "Admin, ContentManager")]
    public class AdminBlogController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IAuthService _authService;

        public AdminBlogController(IBlogService blogService, IAuthService authService)
        {
            _blogService = blogService;
            _authService = authService;
        }

        // SỬA LẠI: Hàm GetById cho Admin
        // Endpoint này dùng để trả về 201 Created
        [HttpGet("posts/{postId}", Name = "AdminGetPostById")] // Đặt tên Route
        public async Task<IActionResult> AdminGetPostById(int postId)
        {
            var post = await _blogService.AdminGetPostByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "Không tìm thấy bài viết." });
            }
            return Ok(post);
        }

        [HttpPost("posts")]
        public async Task<IActionResult> CreatePost([FromBody] AdminBlogCreateDto dto)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _blogService.AdminCreatePostAsync(dto, user.UserId);
            if (result == null)
            {
                return BadRequest(new { message = "Không thể tạo bài viết." });
            }

            // SỬA LẠI: Trỏ đúng tên Route
            return CreatedAtRoute("AdminGetPostById", new { postId = result.PostId }, result);
        }

        [HttpPut("posts/{postId}")]
        public async Task<IActionResult> UpdatePost(int postId, [FromBody] AdminBlogCreateDto dto)
        {
            var result = await _blogService.AdminUpdatePostAsync(postId, dto);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy bài viết." });
            }
            return Ok(result);
        }

        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var success = await _blogService.AdminDeletePostAsync(postId);
            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy bài viết." });
            }
            return Ok(new { message = "Xóa bài viết thành công." });
        }
        [HttpGet("posts")]
        public async Task<IActionResult> AdminGetAllPosts([FromQuery] bool includeDrafts = true)
        {
            var posts = await _blogService.AdminGetAllPostsAsync(includeDrafts);
            return Ok(posts);
        }

        // 1. Lấy danh sách bài đang chờ duyệt
        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _blogService.GetPendingRequestsAsync();
            return Ok(requests);
        }

        // 2. Duyệt bài (Approve)
        [HttpPost("requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            // Lấy ID Admin đang đăng nhập
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var success = await _blogService.ApproveBlogRequestAsync(requestId, user.UserId);

            if (!success) return BadRequest(new { message = "Duyệt thất bại (Bài không tồn tại hoặc đã xử lý)." });

            return Ok(new { message = "Đã duyệt bài thành công! Bài viết đã xuất hiện trên trang chủ." });
        }

        // 3. Từ chối bài (Reject)
        [HttpPost("requests/{requestId}/reject")]
        public async Task<IActionResult> RejectRequest(int requestId, [FromBody] string reason)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var success = await _blogService.RejectBlogRequestAsync(requestId, user.UserId, reason);

            if (!success) return BadRequest(new { message = "Thao tác thất bại." });

            return Ok(new { message = "Đã từ chối bài viết." });
        }
    }
}