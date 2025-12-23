using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Blog;
using CondotelManagement.Models;

namespace CondotelManagement.Services.Interfaces.Blog
{
    public interface IBlogService
    {
        // === Public Methods ===
        Task<IEnumerable<BlogPostSummaryDto>> GetPublishedPostsAsync();
        Task<BlogPostDetailDto?> GetPostBySlugAsync(string slug);
        Task<IEnumerable<BlogCategoryDto>> GetCategoriesAsync();

        // === Admin Post Methods ===
        Task<BlogPostDetailDto?> AdminGetPostByIdAsync(int postId); // THÊM MỚI
        Task<BlogPostDetailDto?> AdminCreatePostAsync(AdminBlogCreateDto dto, int authorUserId);
        Task<BlogPostDetailDto?> AdminUpdatePostAsync(int postId, AdminBlogCreateDto dto);
        Task<bool> AdminDeletePostAsync(int postId);

        // === Admin Category Methods ===
        Task<BlogCategoryDto?> AdminCreateCategoryAsync(BlogCategoryDto dto);
        Task<BlogCategoryDto?> AdminUpdateCategoryAsync(int categoryId, BlogCategoryDto dto);
        Task<bool> AdminDeleteCategoryAsync(int categoryId);
        Task<IEnumerable<BlogPostSummaryDto>> AdminGetAllPostsAsync(bool includeDrafts = true);
        //host
        Task<BlogRequestResultDto> CreateHostBlogRequestAsync(int userId, HostBlogRequestDto dto);
        // === ADMIN APPROVAL ===
        Task<IEnumerable<BlogRequestDetailDto>> GetPendingRequestsAsync(); // Xem danh sách chờ
        Task<bool> ApproveBlogRequestAsync(int requestId, int adminUserId); // Duyệt bài
        Task<bool> RejectBlogRequestAsync(int requestId, int adminUserId, string reason); // Từ chối
        Task<List<HostBlogSummaryDto>> GetHostRequestsAsync(int hostId);
        Task<ServiceResult> ResubmitBlogRequestAsync(int hostId, int requestId, HostBlogRequestDto dto);
        Task<ServiceResult> DeleteBlogRequestAsync(int hostId, int requestId);
        Task<ServiceResult> GetHostBlogRequestDetailAsync(int hostId, int requestId);
    }
}
