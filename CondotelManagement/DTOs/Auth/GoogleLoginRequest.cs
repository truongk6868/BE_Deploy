using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CondotelManagement.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        // Thêm thuộc tính này để ánh xạ JSON camelCase từ FE:
        // C# property: IdToken (PascalCase) <--> JSON payload: idToken (camelCase)
        [JsonPropertyName("idToken")] // 👈 Dòng cần thêm
        [Required]
        public string IdToken { get; set; } = null!;
    }
}