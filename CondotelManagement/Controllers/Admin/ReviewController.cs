using CondotelManagement.Helpers;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
	[Route("api/admin/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")] // QUAN TRỌNG: Chỉ Role "Admin" mới được vào
	public class ReviewController : ControllerBase
	{
		private readonly IReviewService _reviewService;

		public ReviewController(IReviewService reviewService)
		{
			_reviewService = reviewService;
		}
		// Admin xem các review bị report
		[HttpGet("reported")]
		public async Task<IActionResult> GetReported()
			=> Ok(ApiResponse<object>.SuccessResponse(await _reviewService.GetReportedReviewsAsync()));

		// Admin xóa review
		[HttpDelete("{reviewId}")]
		public async Task<IActionResult> DeleteReview(int reviewId)
		{
			await _reviewService.UpdateReviewStatusAsync(reviewId, "Deleted");
			return Ok(ApiResponse<object>.Fail("Đã xóa review"));
		}
	}
}
