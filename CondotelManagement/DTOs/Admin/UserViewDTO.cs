namespace CondotelManagement.DTOs.Admin
{
    public class UserViewDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RoleName { get; set; } // Lấy RoleName cho dễ hiển thị
    }
}
