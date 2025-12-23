using CondotelManagement.DTOs.Package; // Chúng ta sẽ tạo DTOs này
using CondotelManagement.Models;

namespace CondotelManagement.Services.Interfaces
{
    public interface IPackageService
    {
        // (Public) Lấy danh sách gói để hiển thị trang "Bảng giá"
        Task<IEnumerable<PackageDto>> GetAvailablePackagesAsync();

        // (Host) Lấy gói dịch vụ Host đang sử dụng
        Task<HostPackageDetailsDto?> GetMyActivePackageAsync(int hostId);

        // (Host) Mua hoặc nâng cấp gói mới
        Task<HostPackageDetailsDto> PurchaseOrUpgradePackageAsync(int hostId, int packageId);

        // (Host) Hủy package và hoàn tiền
        Task<CancelPackageResponseDTO> CancelPackageAsync(int hostId, CancelPackageRequestDTO request);
    }
}