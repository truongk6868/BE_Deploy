using CondotelManagement.Data;
using CondotelManagement.DTOs.Wallet;
using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces.Wallet;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services.Implementations.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly CondotelDbVer1Context _context;

        public WalletService(CondotelDbVer1Context context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WalletDTO>> GetWalletsByUserIdAsync(int userId)
        {
            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .ToListAsync();

            return wallets.Select(w => new WalletDTO
            {
                WalletId = w.WalletId,
                UserId = w.UserId,
                HostId = w.HostId,
                BankName = w.BankName,
                AccountNumber = w.AccountNumber,
                AccountHolderName = w.AccountHolderName,
                Status = w.Status,
                IsDefault = w.IsDefault
            });
        }

        public async Task<IEnumerable<WalletDTO>> GetWalletsByHostIdAsync(int hostId)
        {
            var wallets = await _context.Wallets
                .Where(w => w.HostId == hostId)
                .ToListAsync();

            return wallets.Select(w => new WalletDTO
            {
                WalletId = w.WalletId,
                UserId = w.UserId,
                HostId = w.HostId,
                BankName = w.BankName,
                AccountNumber = w.AccountNumber,
                AccountHolderName = w.AccountHolderName,
                Status = w.Status,
                IsDefault = w.IsDefault
            });
        }

        public async Task<WalletDTO?> GetWalletByIdAsync(int walletId)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet == null) return null;

            return new WalletDTO
            {
                WalletId = wallet.WalletId,
                UserId = wallet.UserId,
                HostId = wallet.HostId,
                BankName = wallet.BankName,
                AccountNumber = wallet.AccountNumber,
                AccountHolderName = wallet.AccountHolderName,
                Status = wallet.Status,
                IsDefault = wallet.IsDefault
            };
        }

        public async Task<WalletDTO> CreateWalletAsync(WalletCreateDTO dto)
        {
            try
            {
                // Validate: Phải có ít nhất UserId hoặc HostId, nhưng không được cả hai (CHECK constraint CK_Wallet_OneOwner)
                if (!dto.UserId.HasValue && !dto.HostId.HasValue)
                    throw new ArgumentException("Wallet must belong to either user or host");
                
                if (dto.UserId.HasValue && dto.HostId.HasValue)
                    throw new ArgumentException("Wallet cannot belong to both user and host. It must be either user wallet or host wallet.");

                // Nếu set làm default, bỏ default của các wallet khác
                if (dto.IsDefault)
                {
                    // Bỏ default của các wallet có cùng UserId
                    if (dto.UserId.HasValue)
                    {
                        var userWallets = await _context.Wallets
                            .Where(w => w.UserId == dto.UserId && w.WalletId != 0 && w.IsDefault)
                            .ToListAsync();
                        foreach (var w in userWallets)
                        {
                            w.IsDefault = false;
                        }
                    }

                    // Bỏ default của các wallet có cùng HostId
                    if (dto.HostId.HasValue)
                    {
                        var hostWallets = await _context.Wallets
                            .Where(w => w.HostId == dto.HostId && w.WalletId != 0 && w.IsDefault)
                            .ToListAsync();
                        foreach (var w in hostWallets)
                        {
                            w.IsDefault = false;
                        }
                    }
                }

                var wallet = new Models.Wallet
                {
                    UserId = dto.UserId,
                    HostId = dto.HostId,
                    BankName = dto.BankName,
                    AccountNumber = dto.AccountNumber,
                    AccountHolderName = dto.AccountHolderName,
                    Status = "Active",
                    IsDefault = dto.IsDefault
                };

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();

                return new WalletDTO
                {
                    WalletId = wallet.WalletId,
                    UserId = wallet.UserId,
                    HostId = wallet.HostId,
                    BankName = wallet.BankName,
                    AccountNumber = wallet.AccountNumber,
                    AccountHolderName = wallet.AccountHolderName,
                    Status = wallet.Status,
                    IsDefault = wallet.IsDefault
                };
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Re-throw với message rõ ràng hơn
                throw new InvalidOperationException($"Database error while creating wallet: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateWalletAsync(int walletId, WalletUpdateDTO dto)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet == null) return false;

            if (!string.IsNullOrEmpty(dto.BankName))
                wallet.BankName = dto.BankName;
            
            if (!string.IsNullOrEmpty(dto.AccountNumber))
                wallet.AccountNumber = dto.AccountNumber;
            
            if (!string.IsNullOrEmpty(dto.AccountHolderName))
                wallet.AccountHolderName = dto.AccountHolderName;
            
            if (!string.IsNullOrEmpty(dto.Status))
                wallet.Status = dto.Status;
            
            if (dto.IsDefault.HasValue)
            {
                wallet.IsDefault = dto.IsDefault.Value;
                
                // Nếu set làm default, bỏ default của các wallet khác
                if (dto.IsDefault.Value)
                {
                    if (wallet.UserId.HasValue)
                    {
                        var userWallets = await _context.Wallets
                            .Where(w => w.UserId == wallet.UserId && w.WalletId != walletId && w.IsDefault)
                            .ToListAsync();
                        foreach (var w in userWallets)
                        {
                            w.IsDefault = false;
                        }
                    }
                    else if (wallet.HostId.HasValue)
                    {
                        var hostWallets = await _context.Wallets
                            .Where(w => w.HostId == wallet.HostId && w.WalletId != walletId && w.IsDefault)
                            .ToListAsync();
                        foreach (var w in hostWallets)
                        {
                            w.IsDefault = false;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteWalletAsync(int walletId)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            if (wallet == null) return false;

            _context.Wallets.Remove(wallet);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetDefaultWalletAsync(int walletId, int? userId = null, int? hostId = null)
        {
            try
            {
                var wallet = await _context.Wallets.FindAsync(walletId);
                if (wallet == null) return false;

                // Validate wallet thuộc về user hoặc host được chỉ định
                bool isValid = false;

                if (userId.HasValue)
                {
                    // Wallet của user chỉ có UserId, không có HostId
                    if (wallet.UserId.HasValue && wallet.UserId.Value == userId.Value && !wallet.HostId.HasValue)
                        isValid = true;
                }

                if (hostId.HasValue)
                {
                    // Wallet của host chỉ có HostId, không có UserId
                    if (wallet.HostId.HasValue && wallet.HostId.Value == hostId.Value && !wallet.UserId.HasValue)
                        isValid = true;
                }

                if (!isValid)
                    return false;

                // Nếu wallet đã là default, không cần làm gì
                if (wallet.IsDefault)
                    return true;

                // Bỏ default của các wallet khác
                if (userId.HasValue)
                {
                    // Bỏ default của các wallet có cùng UserId
                    var userWallets = await _context.Wallets
                        .Where(w => w.UserId == userId && w.WalletId != walletId && w.IsDefault)
                        .ToListAsync();
                    foreach (var w in userWallets)
                    {
                        w.IsDefault = false;
                    }
                }
                else if (hostId.HasValue)
                {
                    // Bỏ default của các wallet có cùng HostId
                    var hostWallets = await _context.Wallets
                        .Where(w => w.HostId == hostId && w.WalletId != walletId && w.IsDefault)
                        .ToListAsync();
                    foreach (var w in hostWallets)
                    {
                        w.IsDefault = false;
                    }
                }

                wallet.IsDefault = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log exception nếu cần
                return false;
            }
        }
    }
}

