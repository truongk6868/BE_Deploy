using CondotelManagement.DTOs.Host;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.IO;

namespace CondotelManagement.Controllers.Host
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HostController : ControllerBase
    {
        private readonly IHostService _hostService;

        public HostController(IHostService hostService)
        {
            _hostService = hostService;
        }

        /// <summary>
        /// Lấy top 10 host xuất sắc dựa trên đánh giá của khách hàng (Public API - không cần auth)
        /// </summary>
        [HttpGet("top-rated")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTopHostsByRating([FromQuery] int topCount = 10)
        {
            try
            {
                if (topCount <= 0 || topCount > 100)
                {
                    return BadRequest(new { message = "topCount phải từ 1 đến 100." });
                }

                var topHosts = await _hostService.GetTopHostsByRatingAsync(topCount);
                
                return Ok(new
                {
                    success = true,
                    data = topHosts,
                    total = topHosts.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HostController.GetTopHostsByRating] Error: {ex.Message}");
                Console.WriteLine($"[HostController.GetTopHostsByRating] InnerException: {ex.InnerException?.Message}");
                Console.WriteLine($"[HostController.GetTopHostsByRating] Stack: {ex.StackTrace}");
                
                return StatusCode(500, new { 
                    message = $"Lỗi khi lấy danh sách top hosts: {ex.Message}",
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("register-as-host")]
        public async Task<IActionResult> RegisterHost([FromBody] HostRegisterRequestDto dto)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized("Token không hợp lệ.");
                }

                var userId = int.Parse(userIdString);

                // SỬA: Thay đổi biến nhận kết quả thành responseDto (loại bỏ lỗi Serialization)
                var responseDto = await _hostService.RegisterHostAsync(userId, dto);

                return Ok(new
                {
                    message = "Chúc mừng! Bạn đã đăng ký Host thành công.",
                    // Lấy ID từ DTO an toàn
                    hostId = responseDto.HostId
                });
            }
            catch (Exception ex)
            {
                // Lỗi thực tế (bao gồm lỗi SQL UNIQUE KEY cũ nếu chưa xóa) sẽ được trả về
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-with-id-card")]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> VerifyHostWithIdCard([FromForm] HostVerificationRequestDTO request)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { message = "Token không hợp lệ." });
                }

                var userId = int.Parse(userIdString);

                if (request.IdCardFront == null || request.IdCardBack == null)
                {
                    return BadRequest(new { message = "Vui lòng upload đầy đủ ảnh mặt trước và mặt sau CCCD." });
                }

                // Validate file types
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var frontExtension = Path.GetExtension(request.IdCardFront.FileName).ToLowerInvariant();
                var backExtension = Path.GetExtension(request.IdCardBack.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(frontExtension) || !allowedExtensions.Contains(backExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh định dạng JPG, JPEG hoặc PNG." });
                }

                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (request.IdCardFront.Length > maxFileSize || request.IdCardBack.Length > maxFileSize)
                {
                    return BadRequest(new { message = "Kích thước file không được vượt quá 5MB." });
                }

                var result = await _hostService.VerifyHostWithIdCardAsync(userId, request.IdCardFront, request.IdCardBack);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi xác minh: {ex.Message}" });
            }
        }

        [HttpGet("validate-id-card")]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> ValidateIdCard()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { message = "Token không hợp lệ." });
                }

                var userId = int.Parse(userIdString);

                var result = await _hostService.ValidateIdCardInfoAsync(userId);

                if (result.IsValid)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi xác thực: {ex.Message}" });
            }
        }
    }
}