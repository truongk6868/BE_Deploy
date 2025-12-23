using CondotelManagement.DTOs.Admin;

namespace CondotelManagement.Services.Interfaces.Admin
{
    public interface IUserService
    {
        Task<IEnumerable<UserViewDTO>> AdminGetAllUsersAsync();
        Task<UserViewDTO> AdminGetUserByIdAsync(int userId);
        Task<(bool IsSuccess, string Message, UserViewDTO CreatedUser)> AdminCreateUserAsync(AdminCreateUserDTO dto);
        Task<(bool IsSuccess, string Message, UserViewDTO UpdatedUser)> AdminUpdateUserAsync(int userId, AdminUpdateUserDTO dto);
        Task<bool> AdminResetPasswordAsync(int userId, string newPassword);
        Task<bool> AdminUpdateUserStatusAsync(int userId, string newStatus);
    }
}
