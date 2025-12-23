namespace CondotelManagement.DTOs.Host
{
    public class ValidateIdCardResponseDTO
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public ValidationDetailsDTO? Details { get; set; }
    }

    public class ValidationDetailsDTO
    {
        public bool NameMatch { get; set; }
        public bool IdNumberMatch { get; set; }
        public bool DateOfBirthMatch { get; set; }
        public bool VietQRVerified { get; set; }
        public string? UserFullName { get; set; }
        public string? IdCardFullName { get; set; }
        public string? UserDateOfBirth { get; set; }
        public string? IdCardDateOfBirth { get; set; }
        public string? IdCardNumber { get; set; }
        public string? VietQRMessage { get; set; }
    }
}

