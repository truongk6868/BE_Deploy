using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Host;
using CondotelManagement.Models;

namespace CondotelManagement.Services.Interfaces
{
    public interface IHostService
    {
        // Define rõ ràng kiểu Host
        CondotelManagement.Models.Host? GetByUserId(int userId);

        Task<bool> CanHostUploadCondotel(int hostId);

        Task<HostRegistrationResponseDto> RegisterHostAsync(int userId, HostRegisterRequestDto dto);

        Task<HostProfileDTO?> GetHostProfileAsync(int userId);

        Task<bool> UpdateHostProfileAsync(int userId, UpdateHostProfileDTO dto);

        Task<HostVerificationResponseDTO> VerifyHostWithIdCardAsync(int userId, IFormFile idCardFront, IFormFile idCardBack);

        Task<ValidateIdCardResponseDTO> ValidateIdCardInfoAsync(int userId);

        Task<List<TopHostDTO>> GetTopHostsByRatingAsync(int topCount = 10);
    }
}
