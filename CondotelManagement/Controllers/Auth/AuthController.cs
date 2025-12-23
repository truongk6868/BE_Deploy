using CondotelManagement.DTOs.Auth;
using CondotelManagement.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondotelManagement.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Helper method để tránh lặp code
        private IActionResult ValidateAndReturn()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors });
            }
            return null!; // không dùng
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng, hoặc tài khoản chưa được kích hoạt." });

            return Ok(result);
        }

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            var result = await _authService.GoogleLoginAsync(request);
            if (result == null)
                return Unauthorized(new { message = "Đăng nhập Google thất bại." });

            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult; // Đây là dòng QUAN TRỌNG NHẤT

            var success = await _authService.RegisterAsync(request);
            if (!success)
                return BadRequest(new { message = "Email đã tồn tại và được kích hoạt." });

            return StatusCode(201, new
            {
                message = "Đăng ký thành công! Vui lòng kiểm tra email để lấy mã OTP xác thực tài khoản."
            });
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            var success = await _authService.VerifyEmailAsync(request);
            if (!success)
                return BadRequest(new { message = "Email không hợp lệ, OTP sai hoặc đã hết hạn." });

            return Ok(new { message = "Xác thực email thành công. Bạn có thể đăng nhập ngay." });
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            var success = await _authService.VerifyOtpAsync(request);
            if (!success)
                return BadRequest(new { message = "OTP không đúng hoặc đã hết hạn." });

            return Ok(new { message = "Xác thực OTP thành công." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            await _authService.ForgotPasswordAsync(request);
            return Ok(new { message = "Nếu email đã đăng ký, bạn sẽ nhận được liên kết đặt lại mật khẩu." });
        }

        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendPasswordResetOtp([FromBody] ForgotPasswordRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            await _authService.SendPasswordResetOtpAsync(request);
            return Ok(new { message = "Nếu email đã đăng ký, mã OTP sẽ được gửi đến email của bạn." });
        }

        [HttpPost("reset-password-with-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            var success = await _authService.ResetPasswordWithOtpAsync(request);
            if (!success)
                return BadRequest(new { message = "Đặt lại mật khẩu thất bại. Email không tồn tại, OTP sai hoặc đã hết hạn." });

            return Ok(new { message = "Đặt lại mật khẩu thành công!" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized();

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

        [HttpGet("admin-check")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminCheck()
        {
            return Ok(new { message = "Chào mừng Admin!" });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new { message = "Đăng xuất thành công" });
        }
        [HttpPost("change-password")]
        [Authorize] // BẮT BUỘC phải đăng nhập (có JWT token)
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var validationResult = ValidateAndReturn();
            if (validationResult != null) return validationResult;

            // Lấy email từ token JWT
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
                return Unauthorized(new { message = "Không xác thực được người dùng" });

            var result = await _authService.ChangePasswordAsync(emailClaim, request.CurrentPassword, request.NewPassword);
            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }
    }
}