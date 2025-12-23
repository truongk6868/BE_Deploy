namespace CondotelManagement.DTOs.Host
{
    public class HostVerificationResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? VerificationStatus { get; set; }
        public IdCardInfoDTO? FrontInfo { get; set; }
        public IdCardInfoDTO? BackInfo { get; set; }
    }

    public class IdCardInfoDTO
    {
        public string? FullName { get; set; }
        public string? IdNumber { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? Address { get; set; }
        public string? IssueDate { get; set; }
        public string? IssuePlace { get; set; }
        public string? ExpiryDate { get; set; }
    }
}


