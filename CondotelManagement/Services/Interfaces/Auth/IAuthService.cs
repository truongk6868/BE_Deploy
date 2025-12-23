using CondotelManagement.DTOs.Auth;
using CondotelManagement.Models; // Cần User model

namespace CondotelManagement.Services.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<object?> LoginAsync(LoginRequest request);
        Task<object?> GoogleLoginAsync(GoogleLoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request); // THÊM MỚI
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<bool> VerifyEmailAsync(VerifyEmailRequest request);
        Task<bool> VerifyOtpAsync(VerifyOtpRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<User?> GetCurrentUserAsync(); // THÊM MỚI
        Task<bool> SendPasswordResetOtpAsync(ForgotPasswordRequest request);
        Task<bool> ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request);
        Task<(bool IsSuccess, string Message)> ChangePasswordAsync(string email, string currentPassword, string newPassword);
    }
}
