namespace CondotelManagement.DTOs.Amenity
{
    public class AmenityResponseDTO
    {
        public int AmenityId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
    }
}

