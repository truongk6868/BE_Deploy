using CondotelManagement.DTOs.Profile;

namespace CondotelManagement.Services.Interfaces
{
    
    public interface IProfileService
    {
        
        Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    }
}