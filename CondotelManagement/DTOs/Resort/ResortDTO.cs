namespace CondotelManagement.DTOs
{
    public class ResortDTO
    {
        public int ResortId { get; set; }
        public int LocationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public LocationDTO? Location { get; set; }
    }
}










