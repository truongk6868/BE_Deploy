namespace CondotelManagement.DTOs.Payment
{
    public class VietQRCitizenVerifyRequest
    {
        public string LegalId { get; set; } = string.Empty; // Số CCCD/CMND
        public string LegalName { get; set; } = string.Empty; // Tên công dân
    }
}


