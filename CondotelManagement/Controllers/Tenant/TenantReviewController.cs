using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CondotelManagement.DTOs.Tenant;
using CondotelManagement.Services.Interfaces.Tenant;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CondotelManagement.Data;


namespace CondotelManagement.Controllers.Tenant
{
    [ApiController]
    [Route("api/tenant/reviews")]
    [Authorize] // Yêu cầu đăng nhập
    public class TenantReviewController : ControllerBase
    {
        private readonly ITenantReviewService _reviewService;
        private readonly ILogger<TenantReviewController> _logger;
        private readonly CondotelDbVer1Context _context;

        public TenantReviewController(
            ITenantReviewService reviewService,
            ILogger<TenantReviewController> logger,
            CondotelDbVer1Context context)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo review mới cho booking đã hoàn thành
        /// POST /api/tenant/reviews
        /// </summary>

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] ReviewDTO dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var review = await _reviewService.CreateReviewAsync(userId, dto);

                return Ok(new
                {
                    success = true,
                    message = "Review created successfully",
                    data = review
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new { message = "An error occurred while creating the review" });
            }
        }

        /// <summary>
        /// Lấy danh sách review của tôi
        /// GET /api/tenant/reviews
        /// </summary>

        [HttpGet]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var userId = GetCurrentUserId();

                if (userId <= 0)
                    return Unauthorized(new { message = "Invalid user" });

                var reviews = await _reviewService.GetMyReviewsAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = reviews,
                    count = reviews.Count
                });
            }
            
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my reviews for user");
                return StatusCode(500, new
                {
                    message = "An error occurred while getting reviews",
                    detail = ex.Message 
                });
            }
        }

        

        /// <summary>
        /// Lấy chi tiết 1 review
        /// GET /api/tenant/reviews/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var review = await _reviewService.GetReviewByIdAsync(id, userId);

                if (review == null)
                {
                    return NotFound(new { message = "Review not found" });
                }

                return Ok(new { success = true, data = review });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting review {id}");
                return StatusCode(500, new { message = "An error occurred while getting review" });
            }
        }

        /// <summary>
        /// Cập nhật review (trong vòng 7 ngày)
        /// PUT /api/tenant/reviews/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDTO dto)
        {
            try
            {
                if (id != dto.ReviewId)
                {
                    return BadRequest(new { message = "Review ID mismatch" });
                }

                var userId = GetCurrentUserId();
                var review = await _reviewService.UpdateReviewAsync(userId, dto);

                return Ok(new
                {
                    success = true,
                    message = "Review updated successfully",
                    data = review
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating review {id}");
                return StatusCode(500, new { message = "An error occurred while updating review" });
            }
        }

        /// <summary>
        /// Xóa review (trong vòng 7 ngày)
        /// DELETE /api/tenant/reviews/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var deleted = await _reviewService.DeleteReviewAsync(id, userId);

                if (!deleted)
                {
                    return NotFound(new { message = "Review not found" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Review deleted successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting review {id}");
                return StatusCode(500, new { message = "An error occurred while deleting review" });
            }
        }

        [HttpGet("condotel/{condotelId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByCondotel(int condotelId, [FromQuery] ReviewQueryDTO query)
        {
            try
            {
                // Set default values nếu không có
                query.Page = query.Page <= 0 ? 1 : query.Page;
                query.PageSize = query.PageSize <= 0 ? 10 : query.PageSize;
                query.PageSize = Math.Min(query.PageSize, 50); // Giới hạn page size

                var (reviews, totalCount) = await _reviewService.GetReviewsByCondotelAsync(condotelId, query);

                return Ok(new
                {
                    success = true,
                    data = reviews,
                    pagination = new
                    {
                        page = query.Page,
                        pageSize = query.PageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
                    }
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "Không tìm thấy condotel")
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reviews for condotel {condotelId}");
                return StatusCode(500, new { message = "An error occurred while getting reviews" });
            }
        }

    


    // Helper method để lấy UserId từ JWT token
    private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }
    }
}