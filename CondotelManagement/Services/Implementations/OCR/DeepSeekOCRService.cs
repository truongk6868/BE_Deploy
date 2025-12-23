using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces.OCR;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CondotelManagement.Services.Implementations.OCR
{
    public class DeepSeekOCRService : IDeepSeekOCRService
    {
        private readonly HttpClient _httpClient;
        private readonly DeepSeekOCRSettings _settings;

        public DeepSeekOCRService(HttpClient httpClient, IOptions<DeepSeekOCRSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<OCRResult> ExtractIdCardInfoAsync(string imageUrl, bool isFront)
        {
            var result = new OCRResult { Success = false };

            try
            {
                // Download image from URL
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                var base64Image = Convert.ToBase64String(imageBytes);

                // Prepare request to DeepSeek OCR API
                var requestBody = new
                {
                    model = "deepseek-ocr",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = isFront 
                                    ? "Hãy đọc thông tin từ ảnh CCCD mặt trước. Trả về JSON với các trường: FullName (Họ và tên), IdNumber (Số CCCD), DateOfBirth (Ngày sinh), Gender (Giới tính), Nationality (Quốc tịch), Address (Địa chỉ thường trú)."
                                    : "Hãy đọc thông tin từ ảnh CCCD mặt sau. Trả về JSON với các trường: IssueDate (Ngày cấp), IssuePlace (Nơi cấp), ExpiryDate (Ngày hết hạn)."
                                },
                                new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                            }
                        }
                    },
                    temperature = 0.1,
                    max_tokens = 1000
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");

                var response = await _httpClient.PostAsync(_settings.ApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"OCR API error: {response.StatusCode} - {responseContent}";
                    return result;
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var choices = jsonResponse.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
                    
                    if (!string.IsNullOrEmpty(messageContent))
                    {
                        // Try to parse JSON from response
                        try
                        {
                            var ocrData = JsonDocument.Parse(messageContent);
                            var root = ocrData.RootElement;

                            if (isFront)
                            {
                                result.FullName = root.TryGetProperty("FullName", out var fn) ? fn.GetString() : null;
                                result.IdNumber = root.TryGetProperty("IdNumber", out var id) ? id.GetString() : null;
                                result.DateOfBirth = root.TryGetProperty("DateOfBirth", out var dob) ? dob.GetString() : null;
                                result.Gender = root.TryGetProperty("Gender", out var gen) ? gen.GetString() : null;
                                result.Nationality = root.TryGetProperty("Nationality", out var nat) ? nat.GetString() : null;
                                result.Address = root.TryGetProperty("Address", out var addr) ? addr.GetString() : null;
                            }
                            else
                            {
                                result.IssueDate = root.TryGetProperty("IssueDate", out var issueDate) ? issueDate.GetString() : null;
                                result.IssuePlace = root.TryGetProperty("IssuePlace", out var issuePlace) ? issuePlace.GetString() : null;
                                result.ExpiryDate = root.TryGetProperty("ExpiryDate", out var expiryDate) ? expiryDate.GetString() : null;
                            }

                            result.RawText = messageContent;
                            result.Success = true;
                        }
                        catch
                        {
                            // If JSON parsing fails, try to extract info from text
                            result.RawText = messageContent;
                            result.Success = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }
}

