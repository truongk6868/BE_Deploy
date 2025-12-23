namespace CondotelManagement.DTOs.Blog
{
    // Dùng cho cả public và admin
    public class BlogCategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }
}