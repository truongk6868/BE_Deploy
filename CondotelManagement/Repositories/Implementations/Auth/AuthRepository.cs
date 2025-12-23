using CondotelManagement.Data;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories.Implementations.Auth
{
    public class AuthRepository : IAuthRepository
    {
        private readonly CondotelDbVer1Context _context;

        public AuthRepository(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
       .Include(u => u.Role)
       .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        }

        // SỬA ĐỔI: Nhận 'string' thay vì 'byte[]'
        public async Task<bool> UpdatePasswordAsync(string email, string newPasswordHash)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            user.PasswordHash = newPasswordHash; // Gán string trực tiếp
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SetPasswordResetTokenAsync(User user, string token, DateTime expiry)
        {
            user.PasswordResetToken = token;
            user.ResetTokenExpires = expiry;
            _context.Users.Update(user); // Đánh dấu là có thay đổi
            return await _context.SaveChangesAsync() > 0;
        }

        // THÊM PHƯƠNG THỨC NÀY:
        public async Task<User?> GetUserByResetTokenAsync(string token)
        {
            return await _context.Users
                .Include(u => u.Role) // Load cả Role nếu cần
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        }
        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User> RegisterAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

    }
}