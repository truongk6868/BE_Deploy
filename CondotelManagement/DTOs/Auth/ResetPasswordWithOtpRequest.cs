using CondotelManagement.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Auth
{
    public class ResetPasswordWithOtpRequest
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mã OTP không được để trống")]
        [Length(6, 6, ErrorMessage = "Mã OTP phải đúng 6 chữ số")]
        public string Otp { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StrongPassword]
        public string NewPassword { get; set; } = null!;
    }
}
