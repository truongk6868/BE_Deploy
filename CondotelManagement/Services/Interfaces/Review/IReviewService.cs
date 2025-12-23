using CondotelManagement.DTOs;
using CondotelManagement.Models;

namespace CondotelManagement.Services
{
	public interface IReviewService
	{
		Task<IEnumerable<ReviewDTO>> GetReviewsByHostAsync(int hostId);
		Task<IEnumerable<ReviewDTO>> GetReportedReviewsAsync();
		Task ReplyToReviewAsync(int reviewId, string reply);
		Task UpdateReviewStatusAsync(int reviewId, string status);
	}
}
