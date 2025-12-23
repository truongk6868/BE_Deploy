// File: Services/Implementations/PackageFeatureService.cs
using Microsoft.EntityFrameworkCore;
using CondotelManagement.Data;
using CondotelManagement.Services.Interfaces;

namespace CondotelManagement.Services.Implementations
{
    public class PackageFeatureService : IPackageFeatureService
    {
        private readonly CondotelDbVer1Context _context;

        public PackageFeatureService(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public int GetMaxListingCount(int packageId)
        {
            var package = _context.Packages
                .AsNoTracking()
                .FirstOrDefault(p => p.PackageId == packageId);

            return package?.MaxListingCount ?? 0;
        }

        public bool CanUseFeaturedListing(int packageId)
        {
            var package = _context.Packages
                .AsNoTracking()
                .FirstOrDefault(p => p.PackageId == packageId);

            return package?.CanUseFeaturedListing ?? false;
        }

        public int GetMaxBlogRequestsPerMonth(int packageId)
        {
            var package = _context.Packages
                .AsNoTracking()
                .FirstOrDefault(p => p.PackageId == packageId);

            return package?.MaxBlogRequestsPerMonth ?? 0;
        }

        public bool IsVerifiedBadgeEnabled(int packageId)
        {
            var package = _context.Packages
                .AsNoTracking()
                .FirstOrDefault(p => p.PackageId == packageId);

            return package?.IsVerifiedBadgeEnabled ?? false;
        }

        public string GetDisplayColorTheme(int packageId)
        {
            var package = _context.Packages
                .AsNoTracking()
                .FirstOrDefault(p => p.PackageId == packageId);

            return !string.IsNullOrEmpty(package?.DisplayColorTheme)
                ? package.DisplayColorTheme
                : "default";
        }

        public int GetPriorityLevel(int packageId)
        {
            var package = _context.Packages
                .AsNoTracking()
                .FirstOrDefault(p => p.PackageId == packageId);

            return package?.PriorityLevel ?? 0;
        }
    }
}