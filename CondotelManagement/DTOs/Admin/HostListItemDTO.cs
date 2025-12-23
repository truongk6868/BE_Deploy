namespace CondotelManagement.DTOs.Admin
{
    public class HostListItemDTO
    {
        public int HostId { get; set; }
        public string HostName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}




