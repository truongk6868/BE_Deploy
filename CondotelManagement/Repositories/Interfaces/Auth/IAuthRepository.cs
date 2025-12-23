using CondotelManagement.Models;

namespace CondotelManagement.Repositories.Interfaces.Auth
{
    public interface IAuthRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<bool> UpdatePasswordAsync(string email, string newPasswordHash);
        Task<bool> CheckEmailExistsAsync(string email); // THÊM MỚI
        Task<User> RegisterAsync(User user); // THÊM MỚI
        Task<bool> SetPasswordResetTokenAsync(User user, string token, DateTime expiry);
        Task<User?> GetUserByResetTokenAsync(string token);
        Task<bool> UpdateUserAsync(User user);

    }
}