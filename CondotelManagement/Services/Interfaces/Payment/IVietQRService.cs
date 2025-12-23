using CondotelManagement.DTOs.Payment;

namespace CondotelManagement.Services.Interfaces.Payment
{
    public interface IVietQRService
    {
        Task<VietQRBankListResponse> GetBanksAsync();
        Task<VietQRCitizenVerifyResponse> VerifyCitizenAsync(string legalId, string legalName);
    }
}
