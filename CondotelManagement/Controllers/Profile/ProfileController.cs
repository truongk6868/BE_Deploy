using CondotelManagement.DTOs.Auth;
using CondotelManagement.DTOs.Profile;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Profile
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // YÊU CẦU PHẢI ĐĂNG NHẬP cho tất cả API trong này
    public class ProfileController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;

        public ProfileController(IAuthService authService, IProfileService profileService)
        {
            _authService = authService;
            _profileService = profileService;
        }

        /// <summary>
        /// Lấy thông tin profile của user đang đăng nhập
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Map User model sang UserProfileDto để trả về an toàn
            // (Đoạn này được chuyển từ AuthController)
            var userProfile = new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = user.Role?.RoleName ?? "User",
                Status = user.Status,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                ImageUrl = user.ImageUrl,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfile);
        }

        /// <summary>
        /// Cập nhật thông tin profile của user đang đăng nhập
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var success = await _profileService.UpdateProfileAsync(user.UserId, request);

            if (!success)
            {
                return NotFound(new { message = "User not found or failed to update." });
            }

            return Ok(new { message = "Profile updated successfully." });
        }
    }
}
