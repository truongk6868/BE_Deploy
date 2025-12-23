using CondotelManagement.DTOs;
using CondotelManagement.Repositories;

namespace CondotelManagement.Services
{
	public class ReviewService : IReviewService
	{
		private readonly IReviewRepository _repo;

		public ReviewService(IReviewRepository repo)
		{
			_repo = repo;
		}

		public async Task<IEnumerable<ReviewDTO>> GetReviewsByHostAsync(int hostId)
		{
			var reviews = await _repo.GetReviewsByHostAsync(hostId);
			return reviews.Select(r => new ReviewDTO
			{
				ReviewId = r.ReviewId,
				CondotelId = r.CondotelId,
				CondotelName = r.Condotel.Name,
				UserId = r.UserId,
				UserName = r.User.FullName,
				UserImageUrl = r.User.ImageUrl, // Avatar của user
				Rating = r.Rating,
				Comment = r.Comment,
				Reply = r.Reply, // Reply của host
				Status = r.Status,
				CreatedAt = r.CreatedAt
			});
		}

		public async Task<IEnumerable<ReviewDTO>> GetReportedReviewsAsync()
		{
			var reviews = await _repo.GetReportedReviewsAsync();
			return reviews.Select(r => new ReviewDTO
			{
				ReviewId = r.ReviewId,
				CondotelName = r.Condotel.Name,
				UserName = r.User.FullName,
				UserImageUrl = r.User.ImageUrl, // Avatar của user
				Comment = r.Comment,
				Reply = r.Reply, // Reply của host (nếu có)
				Status = r.Status
			});
		}

		public async Task ReplyToReviewAsync(int reviewId, string reply)
		{
			var review = await _repo.GetByIdAsync(reviewId)
				?? throw new Exception("Không tìm thấy review");
			review.Reply = reply;
			await _repo.UpdateAsync(review);
		}

		public async Task UpdateReviewStatusAsync(int reviewId, string status)
		{
			var review = await _repo.GetByIdAsync(reviewId)
				?? throw new Exception("Không tìm thấy review");
			review.Status = status;
			await _repo.UpdateAsync(review);
		}
	}
}
