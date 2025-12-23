namespace CondotelManagement.DTOs.Admin
{
    public class UpdateUserStatusDTO
    {
        // Status này phải khớp với các giá trị bạn quy định
        // (ví dụ: "Active", "Locked", "Deleted")
        public string Status { get; set; }
    }
}
