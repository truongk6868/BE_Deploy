namespace CondotelManagement.DTOs.Blog
{
    public class BlogRequestDetailDto
    {
        public int BlogRequestId { get; set; }
        public int HostId { get; set; }
        public string HostName { get; set; } // Tên công ty hoặc tên Host
        public string Title { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public string FeaturedImageUrl { get; set; }
        public string CategoryName { get; set; }

    }
}