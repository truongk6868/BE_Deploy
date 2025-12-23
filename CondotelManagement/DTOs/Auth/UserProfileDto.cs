namespace CondotelManagement.DTOs.Auth
{
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string RoleName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}