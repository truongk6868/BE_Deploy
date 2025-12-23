using CondotelManagement.DTOs.Profile;
using CondotelManagement.Repositories.Interfaces.Admin;
using CondotelManagement.Services.Interfaces;

namespace CondotelManagement.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepo;

        public ProfileService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Map các trường từ DTO sang Model
            user.FullName = request.FullName;
            user.Phone = request.Phone;
            user.Gender = request.Gender;
            user.DateOfBirth = request.DateOfBirth;
            user.Address = request.Address;

            // Không map: Email, Password, RoleId, Status

            return await _userRepo.UpdateUserAsync(user);
        }
    }
}
