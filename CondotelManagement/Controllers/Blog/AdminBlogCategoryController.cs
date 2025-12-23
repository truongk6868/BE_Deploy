using CondotelManagement.DTOs.Blog;
using CondotelManagement.Services.Interfaces.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
    [Route("api/admin/blog/categories")]
    [ApiController]
    [Authorize(Roles = "Admin, ContentManager")] // Chỉ Admin hoặc ContentManager
    public class AdminBlogCategoryController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public AdminBlogCategoryController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        // Lấy tất cả category (dùng cho admin)
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _blogService.GetCategoriesAsync();
            return Ok(categories);
        }

        // Tạo category
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] BlogCategoryDto dto)
        {
            // Dùng chung DTO, chỉ cần Name
            var newCategory = await _blogService.AdminCreateCategoryAsync(new BlogCategoryDto { Name = dto.Name });
            if (newCategory == null)
            {
                return BadRequest(new { message = "Tạo danh mục thất bại." });
            }
            return CreatedAtAction(nameof(GetCategories), new { id = newCategory.CategoryId }, newCategory);
        }

        // Cập nhật category
        [HttpPut("{categoryId}")]
        public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] BlogCategoryDto dto)
        {
            var updatedCategory = await _blogService.AdminUpdateCategoryAsync(categoryId, dto);
            if (updatedCategory == null)
            {
                return NotFound(new { message = "Không tìm thấy danh mục." });
            }
            return Ok(updatedCategory);
        }

        // Xóa category
        [HttpDelete("{categoryId}")]
        [Authorize(Roles = "Admin")] // Chỉ Admin mới được xóa
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var success = await _blogService.AdminDeleteCategoryAsync(categoryId);
            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy danh mục." });
            }
            return Ok(new { message = "Xóa danh mục thành công." });
        }
    }
}