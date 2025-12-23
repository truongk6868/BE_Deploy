namespace CondotelManagement.DTOs.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? ImageUrl { get; set; }
    }
}
