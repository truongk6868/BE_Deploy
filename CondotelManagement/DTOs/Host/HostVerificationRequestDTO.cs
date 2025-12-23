using Microsoft.AspNetCore.Http;

namespace CondotelManagement.DTOs.Host
{
    public class HostVerificationRequestDTO
    {
        public IFormFile IdCardFront { get; set; } = null!;
        public IFormFile IdCardBack { get; set; } = null!;
    }
}


