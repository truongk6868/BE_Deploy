using CondotelManagement.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces.Admin;

namespace CondotelManagement.Controllers.Admin
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")] // QUAN TRỌNG: Chỉ Role "Admin" mới được vào
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userList = await _userService.AdminGetAllUsersAsync();
            return Ok(userList);
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var user = await _userService.AdminGetUserByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy user");
            return Ok(user);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDTO createUserDto)
        {
            // 🚨 THÊM: Kiểm tra validation trước khi gọi service
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = errors,
                    statusCode = 400
                });
            }

            // 🚨 THÊM: Kiểm tra nếu DTO null
            if (createUserDto == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    statusCode = 400
                });
            }

            // Gọi service
            var result = await _userService.AdminCreateUserAsync(createUserDto);

            if (!result.IsSuccess)
            {
                // Trả về object cho nhất quán
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    statusCode = 400
                });
            }

            // SỬA ĐỔI: Trả về 201 với thông báo và user object
            return CreatedAtAction(
                nameof(GetUserById), // Hàm để lấy lại user
                new { userId = result.CreatedUser.UserId }, // Tham số cho hàm GetUserById
                new
                {
                    success = true,
                    message = result.Message, // Thông báo: "Tạo user thành công. Mã OTP..."
                    data = result.CreatedUser, // Đối tượng user vừa tạo (đang "Pending")
                    statusCode = 201
                }
            );
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] AdminUpdateUserDTO updateUserDto)
        {
            // 1. VALIDATION: Kiểm tra ModelState trước
            if (!ModelState.IsValid)
            {
                // Lấy tất cả error messages
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // Trả về format chuẩn
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ",
                    errors = errors,
                    status = 400
                });
            }

            // 2. Gọi service
            var result = await _userService.AdminUpdateUserAsync(userId, updateUserDto);

            if (!result.IsSuccess)
            {
                // 3. Trả về object thay vì string
                return BadRequest(new
                {
                    message = result.Message,
                    status = 400
                });
            }

            // 4. Trả về success với format chuẩn
            return Ok(new
            {
                message = "Cập nhật thành công",
                data = result.UpdatedUser,
                status = 200
            });
        }

        [HttpPatch("users/{userId}/reset-password")]
        public async Task<IActionResult> AdminResetPassword(int userId, [FromBody] AdminResetPasswordDTO resetPasswordDto)
        {
            var result = await _userService.AdminResetPasswordAsync(userId, resetPasswordDto.NewPassword);
            if (!result)
            {
                return NotFound("Không tìm thấy user hoặc lỗi khi reset");
            }
            return Ok("Đặt lại mật khẩu thành công");
        }

        [HttpPatch("users/{userId}/status")]
        public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusDTO statusDto)
        {
            var result = await _userService.AdminUpdateUserStatusAsync(userId, statusDto.Status);
            if (!result)
            {
                return BadRequest("Cập nhật trạng thái thất bại hoặc không tìm thấy user");
            }
            return Ok($"Cập nhật trạng thái user thành '{statusDto.Status}' thành công");
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            // Đây là xóa mềm (Soft Delete)
            var result = await _userService.AdminUpdateUserStatusAsync(userId, "Deleted");
            if (!result)
            {
                return NotFound("Không tìm thấy user");
            }
            return NoContent(); // 204 No Content
        }
    }
}
