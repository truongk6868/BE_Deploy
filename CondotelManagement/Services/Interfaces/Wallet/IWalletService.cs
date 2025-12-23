using CondotelManagement.DTOs.Wallet;

namespace CondotelManagement.Services.Interfaces.Wallet
{
    public interface IWalletService
    {
        Task<IEnumerable<WalletDTO>> GetWalletsByUserIdAsync(int userId);
        Task<IEnumerable<WalletDTO>> GetWalletsByHostIdAsync(int hostId);
        Task<WalletDTO?> GetWalletByIdAsync(int walletId);
        Task<WalletDTO> CreateWalletAsync(WalletCreateDTO dto);
        Task<bool> UpdateWalletAsync(int walletId, WalletUpdateDTO dto);
        Task<bool> DeleteWalletAsync(int walletId);
        Task<bool> SetDefaultWalletAsync(int walletId, int? userId = null, int? hostId = null);
    }
}

