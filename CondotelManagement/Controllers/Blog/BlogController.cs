using CondotelManagement.Services.Interfaces;
using CondotelManagement.Services.Interfaces.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers
{
    [Route("api/blog")]
    [ApiController]
    [AllowAnonymous] // Cho phép tất cả mọi người truy cập
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public BlogController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [HttpGet("posts")]
        public async Task<IActionResult> GetPublishedPosts()
        {
            var posts = await _blogService.GetPublishedPostsAsync();
            return Ok(posts);
        }

        [HttpGet("posts/{slug}")]
        public async Task<IActionResult> GetPostBySlug(string slug)
        {
            var post = await _blogService.GetPostBySlugAsync(slug);
            if (post == null)
            {
                return NotFound(new { message = "Bài viết không tồn tại hoặc chưa được xuất bản." });
            }
            return Ok(post);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _blogService.GetCategoriesAsync();
            return Ok(categories);
        }
    }
}