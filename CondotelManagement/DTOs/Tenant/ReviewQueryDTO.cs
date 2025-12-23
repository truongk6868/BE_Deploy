namespace CondotelManagement.DTOs.Tenant
{
    public class ReviewQueryDTO
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? MinRating { get; set; }
        public string? SortBy { get; set; } = "date"; 
        public bool? SortDescending { get; set; } = true;
    }
}
