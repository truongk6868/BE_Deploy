namespace CondotelManagement.DTOs
{
    public class LocationDTO
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}
