using CondotelManagement.DTOs.Tenant;

namespace CondotelManagement.Services.Interfaces.Tenant
{
    public interface ITenantReviewService
    {
        /// <summary>
        /// Tạo review mới (chỉ sau khi hoàn thành booking)
        /// </summary>
        Task<ReviewResponseDTO> CreateReviewAsync(int userId, ReviewDTO dto);

        /// <summary>
        /// Lấy danh sách review của tenant hiện tại
        /// </summary>

      Task<List<ReviewResponseDTO>> GetMyReviewsAsync(int userId);

        /// <summary>
        /// Lấy chi tiết 1 review
        /// </summary>
        Task<ReviewResponseDTO?> GetReviewByIdAsync(int reviewId, int userId);

        /// <summary>
        /// Cập nhật review (trong vòng 7 ngày)
        /// </summary>
        Task<ReviewResponseDTO> UpdateReviewAsync(int userId, UpdateReviewDTO dto);

        /// <summary>
        /// Xóa review (trong vòng 7 ngày)
        /// </summary>
        Task<bool> DeleteReviewAsync(int reviewId, int userId);
        Task<(List<ReviewResponseDTO> Reviews, int TotalCount)> GetReviewsByCondotelAsync(int condotelId, ReviewQueryDTO query);

    }
}
