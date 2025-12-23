using CondotelManagement.Data;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Implementations; // 👈 Kế thừa từ file Repository.cs
using CondotelManagement.Repositories.Interfaces;
using CondotelManagement.Repositories.Interfaces.Admin;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories.Implementations.Admin
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        // SỬA LỖI CS7036: Thêm ": base(context)"
        public UserRepository(CondotelDbVer1Context context) : base(context)
        {
        }

        // Phương thức này implement IUserRepository.GetByIdAsync (nullable return)
        // và hide Repository<User>.GetByIdAsync (non-nullable return)
        // Dùng 'new' để tránh warning CS0108
        // Warning CS8613 về nullability là do interface conflict (IRepository vs IUserRepository)
        // - không phải lỗi, có thể bỏ qua
        public new async Task<User?> GetByIdAsync(int userId)
        {
            // Sử dụng _context từ base class (protected field)
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}