using CondotelManagement.Models;

namespace CondotelManagement.Repositories.Interfaces.Admin
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByIdAsync(int userId);
        Task<bool> UpdateUserAsync(User user);
    }
}
