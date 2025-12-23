using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs.Payment
{
    public class GenerateQRRequestDTO
    {
        [Required(ErrorMessage = "Bank code is required")]
        public string BankCode { get; set; } = string.Empty; // MB, VCB, TCB, etc.

        [Required(ErrorMessage = "Account number is required")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(1000, int.MaxValue, ErrorMessage = "Amount must be at least 1,000 VND")]
        public int Amount { get; set; }

        [Required(ErrorMessage = "Account holder name is required")]
        public string AccountHolderName { get; set; } = string.Empty;

        public string? Content { get; set; } // Nội dung chuyển khoản
    }

    public class GenerateQRResponseDTO
    {
        public string QrCodeUrl { get; set; } = string.Empty;
        public string QrCodeUrlCompact { get; set; } = string.Empty;
        public string QrCodeUrlPrint { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string AccountHolderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}






