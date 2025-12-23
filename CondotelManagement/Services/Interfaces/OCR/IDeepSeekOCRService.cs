namespace CondotelManagement.Services.Interfaces.OCR
{
    public interface IDeepSeekOCRService
    {
        Task<OCRResult> ExtractIdCardInfoAsync(string imageUrl, bool isFront);
    }

    public class OCRResult
    {
        public bool Success { get; set; }
        public string? FullName { get; set; }
        public string? IdNumber { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? Address { get; set; }
        public string? IssueDate { get; set; }
        public string? IssuePlace { get; set; }
        public string? ExpiryDate { get; set; }
        public string? RawText { get; set; }
        public string? ErrorMessage { get; set; }
    }
}


