using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // 👈 Cần thêm thư viện này

namespace CondotelManagement.DTOs.Host
{
    // DTO này chứa các thông tin BỔ SUNG mà bảng [Host] cần
    public class HostRegisterRequestDto
    {
        [JsonPropertyName("PhoneContact")]     // ← FE gửi PascalCase
        [Required(ErrorMessage = "Số điện thoại liên hệ là bắt buộc.")]
        public string PhoneContact { get; set; } = null!;

        [JsonPropertyName("Address")]
        public string? Address { get; set; }

        [JsonPropertyName("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("BankName")]
        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc.")]
        public string BankName { get; set; } = null!;

        [JsonPropertyName("AccountNumber")]
        [Required(ErrorMessage = "Số tài khoản là bắt buộc.")]
        public string AccountNumber { get; set; } = null!;

        [JsonPropertyName("AccountHolderName")]
        [Required(ErrorMessage = "Tên chủ tài khoản là bắt buộc.")]
        public string AccountHolderName { get; set; } = null!;
    }
}