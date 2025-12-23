namespace CondotelManagement.DTOs.Host
{
    public class TopHostDTO
    {
        public int HostId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalCondotels { get; set; }
        public int Rank { get; set; }
    }
}


