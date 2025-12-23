namespace CondotelManagement.DTOs
{
    public class ResortCreateUpdateDTO
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Address { get; set; }
    }
}

