using CondotelManagement.Data;
using CondotelManagement.DTOs.Tenant;
using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services.Implementations.Tenant
{
    public class TenantReviewService : ITenantReviewService
    {
        private readonly CondotelDbVer1Context _context;
        private readonly ILogger<TenantReviewService> _logger;
        private const int EDIT_WINDOW_DAYS = 7; // Chỉ được edit trong 7 ngày

        public TenantReviewService(CondotelDbVer1Context context, ILogger<TenantReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReviewResponseDTO> CreateReviewAsync(int userId, ReviewDTO dto)
        {
            // 1. Validate booking tồn tại và thuộc về user
            var booking = await _context.Bookings
                .Include(b => b.Condotel)
                .FirstOrDefaultAsync(b => b.BookingId == dto.BookingId && b.CustomerId == userId);

            if (booking == null)
            {
                throw new InvalidOperationException("Booking not found or does not belong to you");
            }

            // 2. Kiểm tra booking đã hoàn thành chưa
            if (booking.Status != "Completed")
            {
                throw new InvalidOperationException("You can only review completed bookings");
            }

            // 3. Nếu status đã là "Completed" thì cho phép review luôn
            // Không cần kiểm tra EndDate vì status "Completed" đã đảm bảo booking đã hoàn thành

            // 4. Lấy CondotelId từ booking (đảm bảo chính xác)
            var condotelId = booking.CondotelId;

            // 5. Kiểm tra đã review chưa (dùng CondotelId từ booking)
            // Chỉ kiểm tra review chưa bị xóa (Status != "Deleted")
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CondotelId == condotelId
                    && r.BookingId == dto.BookingId && r.Status != "Deleted");

            if (existingReview != null)
            {
                throw new InvalidOperationException("You have already reviewed this booking");
            }

            // 6. Tạo review mới (dùng CondotelId từ booking)
            var review = new Review
            {
                CondotelId = condotelId,
                UserId = userId,
                BookingId = dto.BookingId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} created review {review.ReviewId} for Condotel {condotelId} (Booking {dto.BookingId})");

            return MapToResponseDTO(review, booking.Condotel.Name, true, true);
        }

        public async Task<List<ReviewResponseDTO>> GetMyReviewsAsync(int userId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Condotel)
                .Include(r => r.User) // Include User để lấy ImageUrl
                .Where(r => r.UserId == userId && r.Status != "Deleted") // Không hiển thị review đã bị xóa
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(); 

            
            return reviews.Select(r => new ReviewResponseDTO
            {
                ReviewId = r.ReviewId,
                CondotelId = r.CondotelId,
                CondotelName = r.Condotel.Name,
                UserId = r.UserId,
                UserFullName = r.User?.FullName ?? "Unknown",
                UserImageUrl = r.User?.ImageUrl, // Avatar của user
                Rating = r.Rating,
                Comment = r.Comment,
                Reply = r.Reply, // Reply của host
                CreatedAt = r.CreatedAt,
                CanEdit = CanEditReview(r.CreatedAt),
                CanDelete = CanEditReview(r.CreatedAt)
            }).ToList();
        }


        public async Task<ReviewResponseDTO?> GetReviewByIdAsync(int reviewId, int userId)
        {
            var review = await _context.Reviews
                .Include(r => r.Condotel)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId && r.Status != "Deleted");

            if (review == null) return null;

            var canEdit = CanEditReview(review.CreatedAt);
            return MapToResponseDTO(review, review.Condotel.Name, canEdit, canEdit);
        }

        public async Task<ReviewResponseDTO> UpdateReviewAsync(int userId, UpdateReviewDTO dto)
        {
            var review = await _context.Reviews
                .Include(r => r.Condotel)
                .FirstOrDefaultAsync(r => r.ReviewId == dto.ReviewId && r.UserId == userId);

            if (review == null)
            {
                throw new InvalidOperationException("Review not found or does not belong to you");
            }

            // Kiểm tra còn trong thời gian edit không
            if (!CanEditReview(review.CreatedAt))
            {
                throw new InvalidOperationException($"You can only edit reviews within {EDIT_WINDOW_DAYS} days of creation");
            }

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} updated review {review.ReviewId}");

            return MapToResponseDTO(review, review.Condotel.Name, false, true);
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, int userId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);

            if (review == null) return false;

            // Kiểm tra còn trong thời gian delete không
            if (!CanEditReview(review.CreatedAt))
            {
                throw new InvalidOperationException($"You can only delete reviews within {EDIT_WINDOW_DAYS} days of creation");
            }

            // Không xóa thật, chỉ chuyển status sang "Deleted" để ẩn review
            review.Status = "Deleted";
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} deleted review {reviewId} (status changed to Deleted)");

            return true;
        }

        public async Task<(List<ReviewResponseDTO> Reviews, int TotalCount)> GetReviewsByCondotelAsync(int condotelId, ReviewQueryDTO query)
        {
            // Validate condotel exists
            var condotelExists = await _context.Condotels.AnyAsync(c => c.CondotelId == condotelId);
            if (!condotelExists)
            {
                throw new InvalidOperationException("Không tìm thấy condotel");
            }

            // Base query - chỉ lấy review có status != "Deleted"
            var reviewsQuery = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Condotel)
                .Where(r => r.CondotelId == condotelId && r.Status != "Deleted");

            // Apply filtering by rating if provided
            if (query.MinRating.HasValue && query.MinRating > 0)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Rating >= query.MinRating.Value);
            }

            // Apply sorting
            switch (query.SortBy?.ToLower())
            {
                case "rating":
                    reviewsQuery = query.SortDescending ?? false
                        ? reviewsQuery.OrderByDescending(r => r.Rating)
                        : reviewsQuery.OrderBy(r => r.Rating);
                    break;
                case "date":
                default:
                    reviewsQuery = (query.SortDescending ?? false)
                        ? reviewsQuery.OrderByDescending(r => r.CreatedAt)
                        : reviewsQuery.OrderBy(r => r.CreatedAt);
                    break;
            }

            // Get total count before pagination
            var totalCount = await reviewsQuery.CountAsync();

            // Apply pagination
            var reviews = await reviewsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // Map to DTO
            var reviewDTOs = reviews.Select(r => new ReviewResponseDTO
            {
                ReviewId = r.ReviewId,
                CondotelId = r.CondotelId,
                CondotelName = r.Condotel?.Name ?? "Unknown",
                UserId = r.UserId,
                UserFullName = r.User?.FullName ?? "Anonymous",
                UserImageUrl = r.User?.ImageUrl, // Avatar của user
                Rating = r.Rating,
                Comment = r.Comment,
                Reply = r.Reply, // Reply của host
                CreatedAt = r.CreatedAt,
                CanEdit = false,
                CanDelete = false
            }).ToList();

            return (reviewDTOs, totalCount);
        }
        // Helper methods
        private bool CanEditReview(DateTime createdAt)
        {
            return (DateTime.Now - createdAt).TotalDays <= EDIT_WINDOW_DAYS;
        }

        private ReviewResponseDTO MapToResponseDTO(Review review, string condotelName, bool canEdit, bool canDelete)
        {
            return new ReviewResponseDTO
            {
                ReviewId = review.ReviewId,
                CondotelId = review.CondotelId,
                CondotelName = condotelName,
                UserId = review.UserId,
                UserFullName = review.User?.FullName ?? "Unknown",
                UserImageUrl = review.User?.ImageUrl, // Avatar của user
                Rating = review.Rating,
                Comment = review.Comment,
                Reply = review.Reply, // Reply của host
                CreatedAt = review.CreatedAt,
                CanEdit = canEdit,
                CanDelete = canDelete
            };
        }
    }
}
