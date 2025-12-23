namespace CondotelManagement.DTOs
{
    public class ServicePackageDTO
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
    }
}
