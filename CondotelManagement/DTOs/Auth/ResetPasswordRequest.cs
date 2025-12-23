using CondotelManagement.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StrongPassword]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token đặt lại mật khẩu không được để trống")]
        public string ResetToken { get; set; } = string.Empty;
    }
}
