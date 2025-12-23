namespace CondotelManagement.Models
{
    public class DeepSeekOCRSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = "https://api.deepseek.com/v1/chat/completions";
    }
}


