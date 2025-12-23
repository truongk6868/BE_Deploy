namespace CondotelManagement.DTOs.Tenant
{
    public class ReviewResponseDTO
    {
        public int ReviewId { get; set; }
        public int CondotelId { get; set; }
        public string CondotelName { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public string? UserImageUrl { get; set; } // Avatar của user
        public byte Rating { get; set; }
        public string? Comment { get; set; }
        public string? Reply { get; set; } // Reply của host
        public DateTime CreatedAt { get; set; }
        public bool CanEdit { get; set; } // Chỉ edit được trong 7 ngày
        public bool CanDelete { get; set; }
    }
}
