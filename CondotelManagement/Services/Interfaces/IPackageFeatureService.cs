// File: Services/Interfaces/IPackageFeatureService.cs
namespace CondotelManagement.Services.Interfaces
{
    public interface IPackageFeatureService
    {
        // TẤT CẢ PHƯƠNG THỨC ĐỀU ĐỒNG BỘ
        int GetMaxListingCount(int packageId);
        bool CanUseFeaturedListing(int packageId);
        int GetMaxBlogRequestsPerMonth(int packageId);
        bool IsVerifiedBadgeEnabled(int packageId);
        string GetDisplayColorTheme(int packageId);
        int GetPriorityLevel(int packageId);
    }
}