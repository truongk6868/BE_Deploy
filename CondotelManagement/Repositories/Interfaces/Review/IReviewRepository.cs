using CondotelManagement.Models;

namespace CondotelManagement.Repositories {
	public interface IReviewRepository
	{
		Task<IEnumerable<Review>> GetReviewsByHostAsync(int hostId);
		Task<IEnumerable<Review>> GetReportedReviewsAsync();
		Task<Review?> GetByIdAsync(int id);
		Task UpdateAsync(Review review);
	}
}
