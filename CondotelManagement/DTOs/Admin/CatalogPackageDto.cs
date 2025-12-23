namespace CondotelManagement.DTOs.Admin
{
    public class CatalogPackageDto
    {
        public int PackageId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int? DurationDays { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
