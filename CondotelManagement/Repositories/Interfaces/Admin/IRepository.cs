using System.Linq.Expressions;

namespace CondotelManagement.Repositories.Interfaces.Admin
{
    // T là một Model (ví dụ: User, Role)
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);

        // Hàm này rất quan trọng, dùng để tìm kiếm
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    }
}
