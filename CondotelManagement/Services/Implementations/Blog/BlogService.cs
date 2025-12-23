using CondotelManagement.Data;
using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Blog;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces; // Sửa Using
using CondotelManagement.Repositories.Interfaces.Admin;
using CondotelManagement.Services.Interfaces.Blog; // Sửa Using
using Microsoft.EntityFrameworkCore;
using System.Text; // Thêm
using System.Text.RegularExpressions;

namespace CondotelManagement.Services.Implementations.Blog
{
    public class BlogService : IBlogService
    {
        private readonly CondotelDbVer1Context _context;
        private readonly IRepository<BlogPost> _postRepo;
        private readonly IRepository<BlogCategory> _categoryRepo;
        private readonly IRepository<User> _userRepo; // Thêm User Repo

        public BlogService(CondotelDbVer1Context context,
                               IRepository<BlogPost> postRepo,
                               IRepository<BlogCategory> categoryRepo,
                               IRepository<User> userRepo) // Thêm
        {
            _context = context;
            _postRepo = postRepo;
            _categoryRepo = categoryRepo;
            _userRepo = userRepo; // Thêm
        }

        // === Public Methods (Giữ nguyên) ===
        public async Task<IEnumerable<BlogPostSummaryDto>> GetPublishedPostsAsync()
        {
            // ... (Code đã chuẩn) ...
            return await _context.BlogPosts
                .Where(p => p.Status == "Published")
                .Include(p => p.AuthorUser)
                .Include(p => p.Category)
                .OrderByDescending(p => p.PublishedAt)
                .Select(p => new BlogPostSummaryDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Slug = p.Slug,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    PublishedAt = p.PublishedAt,
                    AuthorName = p.AuthorUser.FullName,
                    CategoryName = p.Category != null ? p.Category.Name : "Uncategorized"
                })
                .ToListAsync();
        }

        public async Task<BlogPostDetailDto?> GetPostBySlugAsync(string slug)
        {
            // ... (Code đã chuẩn) ...
            return await _context.BlogPosts
                .Where(p => p.Slug == slug && p.Status == "Published")
                .Include(p => p.AuthorUser)
                .Include(p => p.Category)
                .Select(p => new BlogPostDetailDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Slug = p.Slug,
                    Content = p.Content,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    PublishedAt = p.PublishedAt,
                    AuthorName = p.AuthorUser.FullName,
                    CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                    CategoryId = p.CategoryId, // Thêm
                    Status = p.Status
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<BlogCategoryDto>> GetCategoriesAsync()
        {
            // ... (Code đã chuẩn, dùng _context) ...
            return await _context.BlogCategories
                .Select(c => new BlogCategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Slug = c.Slug
                })
                .ToListAsync();
        }

        // === Admin Post Methods ===

        // THÊM MỚI: Hàm GetById cho Admin
        public async Task<BlogPostDetailDto?> AdminGetPostByIdAsync(int postId)
        {
            return await _context.BlogPosts
               .Where(p => p.PostId == postId) // Lấy cả bài nháp
               .Include(p => p.AuthorUser)
               .Include(p => p.Category)
               .Select(p => new BlogPostDetailDto
               {
                   PostId = p.PostId,
                   Title = p.Title,
                   Slug = p.Slug,
                   Content = p.Content,
                   FeaturedImageUrl = p.FeaturedImageUrl,
                   PublishedAt = p.PublishedAt,
                   AuthorName = p.AuthorUser.FullName,
                   CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                   CategoryId = p.CategoryId, 
                   Status = p.Status
               })
               .FirstOrDefaultAsync();
        }
        public async Task<IEnumerable<BlogPostSummaryDto>> AdminGetAllPostsAsync(bool includeDrafts = true)
        {
            var query = _context.BlogPosts.AsQueryable();

            if (!includeDrafts)
            {
                query = query.Where(p => p.Status == "Published");
            }

            return await query
                .Include(p => p.AuthorUser)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new BlogPostSummaryDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Slug = p.Slug,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    PublishedAt = p.PublishedAt,
                    AuthorName = p.AuthorUser.FullName,
                    CategoryName = p.Category != null ? p.Category.Name : "Uncategorized"
                })
                .ToListAsync();
        }
        public async Task<BlogPostDetailDto?> AdminCreatePostAsync(AdminBlogCreateDto dto, int authorUserId)
        {
            // Sửa: Dùng _context thay vì _categoryRepo.GetByIdAsync
            var category = dto.CategoryId.HasValue ? await _context.BlogCategories.FindAsync(dto.CategoryId.Value) : null;
            var author = await _userRepo.GetByIdAsync(authorUserId); // Dùng User Repo
            if (author == null) return null;

            var slug = await GenerateUniqueSlugAsync(dto.Title);

            var newPost = new BlogPost
            {
                Title = dto.Title,
                Slug = slug,
                Content = dto.Content,
                FeaturedImageUrl = dto.FeaturedImageUrl,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow,
                AuthorUserId = authorUserId,
                CategoryId = category?.CategoryId
            };

            if (newPost.Status == "Published")
            {
                newPost.PublishedAt = DateTime.UtcNow;
            }

            await _postRepo.AddAsync(newPost);

            return new BlogPostDetailDto
            {
                PostId = newPost.PostId,
                Title = newPost.Title,
                Slug = newPost.Slug,
                Content = newPost.Content,
                FeaturedImageUrl = newPost.FeaturedImageUrl,
                PublishedAt = newPost.PublishedAt,
                AuthorName = author.FullName,
                CategoryName = category?.Name ?? "Uncategorized"
            };
        }

        public async Task<BlogPostDetailDto?> AdminUpdatePostAsync(int postId, AdminBlogCreateDto dto)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null) return null;

            var category = dto.CategoryId.HasValue ? await _context.BlogCategories.FindAsync(dto.CategoryId.Value) : null;
            var author = await _userRepo.GetByIdAsync(post.AuthorUserId);

            if (post.Title != dto.Title)
            {
                post.Slug = await GenerateUniqueSlugAsync(dto.Title, postId); // Thêm postId để loại trừ chính nó
            }

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.FeaturedImageUrl = dto.FeaturedImageUrl;
            post.CategoryId = category?.CategoryId;

            if (post.Status != "Published" && dto.Status == "Published")
            {
                post.PublishedAt = DateTime.UtcNow;
            }
            post.Status = dto.Status;

            await _postRepo.UpdateAsync(post);

            return new BlogPostDetailDto
            {
                PostId = post.PostId,
                Title = post.Title,
                Slug = post.Slug,
                Content = post.Content,
                FeaturedImageUrl = post.FeaturedImageUrl,
                PublishedAt = post.PublishedAt,
                AuthorName = author.FullName,
                CategoryName = category?.Name ?? "Uncategorized"
            };
        }

        public async Task<bool> AdminDeletePostAsync(int postId)
        {
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null) return false;
            await _postRepo.DeleteAsync(post);
            return true;
        }

        // === HIỆN THỰC CÁC HÀM CATEGORY ===

        public async Task<BlogCategoryDto?> AdminCreateCategoryAsync(BlogCategoryDto dto)
        {
            var slug = await GenerateUniqueCategorySlugAsync(dto.Name);
            var newCategory = new BlogCategory
            {
                Name = dto.Name,
                Slug = slug
            };

            await _categoryRepo.AddAsync(newCategory);

            dto.CategoryId = newCategory.CategoryId;
            dto.Slug = newCategory.Slug;
            return dto;
        }

        public async Task<BlogCategoryDto?> AdminUpdateCategoryAsync(int categoryId, BlogCategoryDto dto)
        {
            var category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category == null) return null;

            if (category.Name != dto.Name)
            {
                category.Slug = await GenerateUniqueCategorySlugAsync(dto.Name, categoryId);
            }

            category.Name = dto.Name;

            await _categoryRepo.UpdateAsync(category);

            dto.CategoryId = category.CategoryId;
            dto.Slug = category.Slug;
            return dto;
        }

        public async Task<bool> AdminDeleteCategoryAsync(int categoryId)
        {
            // Lưu ý: DB của bạn đã set ON DELETE SET NULL,
            // nên xóa Category là an toàn, bài viết sẽ tự động cập nhật CategoryID = NULL
            var category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category == null) return false;

            await _categoryRepo.DeleteAsync(category);
            return true;
        }


        // === Private Helper ===

        private async Task<string> GenerateUniqueSlugAsync(string title, int? postIdToExclude = null)
        {
            var slug = GenerateSlug(title);
            var originalSlug = slug;
            int count = 1;

            var query = _context.BlogPosts.Where(p => p.Slug == slug);
            if (postIdToExclude.HasValue)
            {
                // Khi update, loại trừ chính bài viết đó
                query = query.Where(p => p.PostId != postIdToExclude.Value);
            }

            while (await query.AnyAsync())
            {
                slug = $"{originalSlug}-{count}";
                count++;

                query = _context.BlogPosts.Where(p => p.Slug == slug);
                if (postIdToExclude.HasValue)
                {
                    query = query.Where(p => p.PostId != postIdToExclude.Value);
                }
            }
            return slug;
        }

        // Helper cho Category Slug
        private async Task<string> GenerateUniqueCategorySlugAsync(string name, int? categoryIdToExclude = null)
        {
            var slug = GenerateSlug(name);
            var originalSlug = slug;
            int count = 1;

            var query = _context.BlogCategories.Where(p => p.Slug == slug);
            if (categoryIdToExclude.HasValue)
            {
                query = query.Where(p => p.CategoryId != categoryIdToExclude.Value);
            }

            while (await query.AnyAsync())
            {
                slug = $"{originalSlug}-{count}";
                count++;

                query = _context.BlogCategories.Where(p => p.Slug == slug);
                if (categoryIdToExclude.HasValue)
                {
                    query = query.Where(p => p.CategoryId != categoryIdToExclude.Value);
                }
            }
            return slug;
        }

        private string GenerateSlug(string phrase)
        {
            string str = RemoveAccents(phrase).ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }

        // SỬA LỖI 3: Hoàn thiện hàm RemoveAccents, xóa bỏ '...'
        private string RemoveAccents(string text)
        {
            var sb = new StringBuilder(text);
            string[,] replacements = {
                {"à","a"}, {"á","a"}, {"ạ","a"}, {"ả","a"}, {"ã","a"}, {"â","a"}, {"ầ","a"}, {"ấ","a"}, {"ậ","a"}, {"ẩ","a"}, {"ẫ","a"}, {"ă","a"}, {"ằ","a"}, {"ắ","a"}, {"ặ","a"}, {"ẳ","a"}, {"ẵ","a"},
                {"è","e"}, {"é","e"}, {"ẹ","e"}, {"ẻ","e"}, {"ẽ","e"}, {"ê","e"}, {"ề","e"}, {"ế","e"}, {"ệ","e"}, {"ể","e"}, {"ễ","e"},
                {"ì","i"}, {"í","i"}, {"ị","i"}, {"ỉ","i"}, {"ĩ","i"},
                {"ò","o"}, {"ó","o"}, {"ọ","o"}, {"ỏ","o"}, {"õ","o"}, {"ô","o"}, {"ồ","o"}, {"ố","o"}, {"ộ","o"}, {"ổ","o"}, {"ỗ","o"}, {"ơ","o"}, {"ờ","o"}, {"ớ","o"}, {"ợ","o"}, {"ở","o"}, {"ỡ","o"},
                {"ù","u"}, {"ú","u"}, {"ụ","u"}, {"ủ","u"}, {"ũ","u"}, {"ư","u"}, {"ừ","u"}, {"ứ","u"}, {"ự","u"}, {"ử","u"}, {"ữ","u"},
                {"ỳ","y"}, {"ý","y"}, {"ỵ","y"}, {"ỷ","y"}, {"ỹ","y"},
                {"đ","d"},
                {"À","A"}, {"Á","A"}, {"Ạ","A"}, {"Ả","A"}, {"Ã","A"}, {"Â","A"}, {"Ầ","A"}, {"Ấ","A"}, {"Ậ","A"}, {"Ẩ","A"}, {"Ẫ","A"}, {"Ă","A"}, {"Ằ","A"}, {"Ắ","A"}, {"Ặ","A"}, {"Ẳ","A"}, {"Ẵ","A"},
                {"È","E"}, {"É","E"}, {"Ẹ","E"}, {"Ẻ","E"}, {"Ẽ","E"}, {"Ê","E"}, {"Ề","E"}, {"Ế","E"}, {"Ệ","E"}, {"Ể","E"}, {"Ễ","E"},
                {"Ì","I"}, {"Í","I"}, {"Ị","I"}, {"Ỉ","I"}, {"Ĩ","I"},
                {"Ò","O"}, {"Ó","O"}, {"Ọ","O"}, {"Ỏ","O"}, {"Õ","O"}, {"Ô","O"}, {"Ồ","O"}, {"Ố","O"}, {"Ộ","O"}, {"Ổ","O"}, {"Ỗ","O"}, {"Ơ","O"}, {"Ờ","O"}, {"Ớ","O"}, {"Ợ","O"}, {"Ở","O"}, {"Ỡ","O"},
                {"Ù","U"}, {"Ú","U"}, {"Ụ","U"}, {"Ủ","U"}, {"Ũ","U"}, {"Ư","U"}, {"Ừ","U"}, {"Ứ","U"}, {"Ự","U"}, {"Ử","U"}, {"Ữ","U"},
                {"Ỳ","Y"}, {"Ý","Y"}, {"Ỵ","Y"}, {"Ỷ","Y"}, {"Ỹ","Y"},
                {"Đ","D"}
            };

            for (int i = 0; i < replacements.GetLength(0); i++)
            {
                sb.Replace(replacements[i, 0], replacements[i, 1]);
            }

            return sb.ToString();
        }
        public async Task<BlogRequestResultDto> CreateHostBlogRequestAsync(int userId, HostBlogRequestDto dto)
        {
            // 1. Lấy thông tin Host từ UserId
            var host = await _context.Hosts.FirstOrDefaultAsync(h => h.UserId == userId);
            if (host == null)
            {
                return new BlogRequestResultDto { Success = false, Message = "Tài khoản của bạn chưa đăng ký làm Host." };
            }

            // 2. Lấy thông tin Gói dịch vụ (Package) hiện tại của Host
            // Logic: Lấy gói Active, còn hạn, có ngày kết thúc xa nhất
            var currentDate = DateOnly.FromDateTime(DateTime.Now);

            var activePackageInfo = await _context.HostPackages
                .Include(hp => hp.Package)
                .Where(hp => hp.HostId == host.HostId
                             && hp.Status == "Active"
                             && hp.EndDate >= currentDate)
                .OrderByDescending(hp => hp.Package.MaxBlogRequestsPerMonth)
                .Select(hp => new {
                    hp.Package.MaxBlogRequestsPerMonth,
                    PackageName = hp.Package.Name
                })
                .FirstOrDefaultAsync();

            int maxAllowed = activePackageInfo?.MaxBlogRequestsPerMonth ?? 0;
            string packageName = activePackageInfo?.PackageName ?? "Chưa đăng ký gói";

            // CHECK 1: Nếu giới hạn = 0 (Gói thường hoặc chưa mua gói)
            if (maxAllowed == 0)
            {
                return new BlogRequestResultDto
                {
                    Success = false,
                    Message = $"Gói dịch vụ hiện tại ({packageName}) không hỗ trợ đăng Blog. Vui lòng nâng cấp gói VIP.",
                    CurrentPackage = packageName,
                    RemainingQuota = 0
                };
            }

            // 3. Đếm số lượng yêu cầu đã gửi trong tháng này
            //var vnTime = DateTime.UtcNow.AddHours(7);
            //int usedCount = await _context.BlogRequests
            //    .CountAsync(br => br.HostId == host.HostId
            //                      && br.RequestDate.Month == vnTime.Month
            //                      && br.RequestDate.Year == vnTime.Year
            //                      && br.Status != "Rejected");
            int usedCount = await _context.BlogRequests
        .CountAsync(br => br.HostId == host.HostId
                          && br.Status != "Rejected");

            // CHECK 2: Nếu đã dùng hết lượt
            if (usedCount >= maxAllowed)
            {
                return new BlogRequestResultDto
                {
                    Success = false,
                    Message = $"Bạn đã đạt giới hạn đăng bài trong tháng ({usedCount}/{maxAllowed}).",
                    CurrentPackage = packageName,
                    RemainingQuota = 0
                };
            }

            // 4. Nếu hợp lệ -> Lưu vào Database (Bảng BlogRequests)
            var newRequest = new BlogRequest
            {
                HostId = host.HostId,
                Title = dto.Title,
                Content = dto.Content,
                FeaturedImageUrl = dto.FeaturedImageUrl,
                CategoryId = dto.CategoryId,
                Status = "Pending",
                RequestDate = DateTime.UtcNow
                // ProcessedDate, ProcessedByUserId để null
            };

            _context.BlogRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            return new BlogRequestResultDto
            {
                Success = true,
                Message = "Gửi yêu cầu duyệt bài thành công!",
                CurrentPackage = packageName,
                RemainingQuota = maxAllowed - usedCount - 1
            };
        }

        // ...
        public async Task<IEnumerable<BlogRequestDetailDto>> GetPendingRequestsAsync()
        {
            return await _context.BlogRequests
                .Where(r => r.Status == "Pending")
                .Include(r => r.Host).ThenInclude(h => h.User)

                // 👇 BÂY GIỜ MỚI CÓ THỂ INCLUDE ĐƯỢC
                .Include(r => r.BlogCategory)

                .OrderByDescending(r => r.RequestDate)
                .Select(r => new BlogRequestDetailDto
                {
                    BlogRequestId = r.BlogRequestId,
                    HostId = r.HostId,
                    HostName = r.Host.User.FullName,
                    Title = r.Title,
                    Content = r.Content,
                    Status = r.Status,
                    RequestDate = r.RequestDate,
                    FeaturedImageUrl = r.FeaturedImageUrl,

                    // 👇 BÂY GIỜ MỚI LẤY ĐƯỢC DỮ LIỆU
                    CategoryName = r.BlogCategory != null ? r.BlogCategory.Name : "Chưa phân loại"
                })
                .ToListAsync();
        }

        public async Task<bool> ApproveBlogRequestAsync(int requestId, int adminUserId)
        {
            try
            {
                // 1. Dùng Include để lấy thông tin Host (TRÁNH LỖI NULL HOST)
                var request = await _context.BlogRequests
                    .Include(r => r.Host)
                    .FirstOrDefaultAsync(r => r.BlogRequestId == requestId);

                if (request == null || request.Status != "Pending") return false;

                // 2. Xử lý Category (TRÁNH LỖI ID = 0)
                // Nếu request không có category, lấy Category đầu tiên trong DB làm mặc định
                int finalCategoryId = request.CategoryId ?? 0;

                // Kiểm tra xem Category có tồn tại không, nếu không thì lấy cái đầu tiên tìm thấy
                if (finalCategoryId == 0 || !await _context.BlogCategories.AnyAsync(c => c.CategoryId == finalCategoryId))
                {
                    var defaultCat = await _context.BlogCategories.FirstOrDefaultAsync();
                    finalCategoryId = defaultCat?.CategoryId ?? 1; // Fallback về 1 nếu vẫn null
                }

                // 3. Tạo bài viết mới
                var newPost = new BlogPost
                {
                    Title = request.Title,
                    Content = request.Content,

                    // Lưu ý: Cần đảm bảo hàm GenerateSlug xử lý trùng lặp (ví dụ thêm số -1, -2 đuôi)
                    // Hoặc đơn giản là cộng thêm ID vào slug để luôn unique
                    Slug = GenerateSlug(request.Title) + "-" + Guid.NewGuid().ToString().Substring(0, 4),

                    FeaturedImageUrl = request.FeaturedImageUrl,
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    PublishedAt = DateTime.UtcNow,

                    // Lấy ID Host an toàn
                    AuthorUserId = request.Host.UserId,

                    CategoryId = finalCategoryId
                };

                // 4. Cập nhật trạng thái Request
                request.Status = "Approved";
                request.ProcessedDate = DateTime.UtcNow;
                request.ProcessedByUserId = adminUserId;

                // 5. Lưu vào DB
                _context.BlogPosts.Add(newPost);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                // In lỗi ra Console của Server để bạn đọc được lý do chính xác
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine($"[ERROR APPROVE] RequestId: {requestId}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine("--------------------------------------------------");
                throw; // Ném lỗi tiếp để Controller bắt được trả về 500
            }
        }

        public async Task<bool> RejectBlogRequestAsync(int requestId, int adminUserId, string reason)
        {
            var request = await _context.BlogRequests.FindAsync(requestId);
            if (request == null || request.Status != "Pending") return false;

            request.Status = "Rejected";
            request.RejectionReason = reason;
            request.ProcessedDate = DateTime.UtcNow;
            request.ProcessedByUserId = adminUserId;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<List<HostBlogSummaryDto>> GetHostRequestsAsync(int hostId)
        {
            return await _context.BlogRequests
                .Where(r => r.HostId == hostId)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new HostBlogSummaryDto
                {
                    Id = r.BlogRequestId,
                    Title = r.Title,
                    Thumbnail = r.FeaturedImageUrl,
                    Status = r.Status, // Pending, Approved, Rejected
                    RejectionReason = r.Status == "Rejected" ? r.RejectionReason : null, // Chỉ hiện lý do nếu bị từ chối
                    CreatedAt = r.RequestDate
                })
                .ToListAsync();
        }
        public async Task<ServiceResult> ResubmitBlogRequestAsync(int hostId, int requestId, HostBlogRequestDto dto)
        {
            var request = await _context.BlogRequests.FirstOrDefaultAsync(r => r.BlogRequestId == requestId);

            // Validate cơ bản
            if (request == null) return new ServiceResult(false, "Không tìm thấy bài viết.");
            if (request.HostId != hostId) return new ServiceResult(false, "Bạn không có quyền sửa bài này.");

            // Chỉ cho sửa khi bài đang Pending hoặc Rejected. 
            // Nếu Approved rồi thì không được sửa (hoặc phải dùng luồng khác).
            if (request.Status == "Approved") return new ServiceResult(false, "Bài đã được duyệt, không thể chỉnh sửa.");

            // Cập nhật thông tin
            request.Title = dto.Title;
            request.Content = dto.Content;
            request.FeaturedImageUrl = dto.FeaturedImageUrl;
            request.CategoryId = dto.CategoryId;

            // QUAN TRỌNG: Reset trạng thái để Admin duyệt lại
            request.Status = "Pending";
            request.RejectionReason = null; // Xóa lý do từ chối cũ
            request.RequestDate = DateTime.UtcNow; // Cập nhật lại ngày gửi để nó nhảy lên đầu danh sách Admin

            await _context.SaveChangesAsync();
            return new ServiceResult(true, "Đã cập nhật và gửi lại yêu cầu duyệt.");
        }
        public async Task<ServiceResult> DeleteBlogRequestAsync(int hostId, int requestId)
        {
            // 1. Tìm bài viết
            var request = await _context.BlogRequests.FirstOrDefaultAsync(r => r.BlogRequestId == requestId);

            if (request == null)
                return new ServiceResult(false, "Không tìm thấy bài viết.");

            // 2. Kiểm tra chính chủ
            if (request.HostId != hostId)
                return new ServiceResult(false, "Bạn không có quyền xóa bài này.");

            // 3. Kiểm tra trạng thái
            // Chỉ cho xóa khi đang chờ duyệt (Pending) hoặc bị từ chối (Rejected).
            // Nếu đã duyệt (Approved) thì không cho xóa (hoặc tùy nghiệp vụ của bạn).
            if (request.Status == "Approved")
                return new ServiceResult(false, "Không thể xóa bài đã được duyệt.");

            // 4. Xóa
            _context.BlogRequests.Remove(request);
            await _context.SaveChangesAsync();

            return new ServiceResult(true, "Đã xóa bài viết thành công.");
        }
        public async Task<ServiceResult> GetHostBlogRequestDetailAsync(int hostId, int requestId)
        {
            var request = await _context.BlogRequests
                .Where(r => r.BlogRequestId == requestId && r.HostId == hostId)
                .Select(r => new HostBlogRequestDto
                {
                    Title = r.Title,
                    Content = r.Content,
                    FeaturedImageUrl = r.FeaturedImageUrl,
                    CategoryId = r.CategoryId
                })
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return new ServiceResult(false, "Không tìm thấy yêu cầu hoặc bạn không có quyền truy cập.");
            }

            // Kiểm tra trạng thái: không cho edit nếu đã Approved
            var status = await _context.BlogRequests
                .Where(r => r.BlogRequestId == requestId)
                .Select(r => r.Status)
                .FirstOrDefaultAsync();

            if (status == "Approved")
            {
                return new ServiceResult(false, "Bài viết đã được duyệt, không thể chỉnh sửa.");
            }

            // Thành công → trả về data
            return new ServiceResult(true, "Thành công", request);
        }
    }
}