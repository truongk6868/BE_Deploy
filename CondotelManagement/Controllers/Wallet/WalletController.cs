using CondotelManagement.DTOs.Wallet;
using CondotelManagement.Services.Interfaces.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondotelManagement.Controllers.Wallet
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Lấy danh sách tài khoản ngân hàng của user hiện tại
        /// GET /api/wallet/my-wallets
        /// </summary>
        [HttpGet("my-wallets")]
        public async Task<IActionResult> GetMyWallets()
        {
            var userId = GetCurrentUserId();
            var wallets = await _walletService.GetWalletsByUserIdAsync(userId);
            
            return Ok(new
            {
                success = true,
                data = wallets,
                total = wallets.Count()
            });
        }

        /// <summary>
        /// Lấy tài khoản ngân hàng theo ID
        /// GET /api/wallet/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWalletById(int id)
        {
            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null)
                return NotFound(new { success = false, message = "Wallet not found" });

            // Kiểm tra quyền truy cập
            var userId = GetCurrentUserId();
            if (wallet.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            return Ok(new { success = true, data = wallet });
        }

        /// <summary>
        /// Tạo tài khoản ngân hàng mới
        /// POST /api/wallet
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromBody] WalletCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var userId = GetCurrentUserId();
            dto.UserId = userId; // Set userId từ token

            var created = await _walletService.CreateWalletAsync(dto);
            return CreatedAtAction(nameof(GetWalletById), new { id = created.WalletId }, new
            {
                success = true,
                message = "Wallet created successfully",
                data = created
            });
        }

        /// <summary>
        /// Cập nhật tài khoản ngân hàng
        /// PUT /api/wallet/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWallet(int id, [FromBody] WalletUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null)
                return NotFound(new { success = false, message = "Wallet not found" });

            // Kiểm tra quyền truy cập
            var userId = GetCurrentUserId();
            if (wallet.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var updated = await _walletService.UpdateWalletAsync(id, dto);
            if (!updated)
                return BadRequest(new { success = false, message = "Failed to update wallet" });

            return Ok(new { success = true, message = "Wallet updated successfully" });
        }

        /// <summary>
        /// Xóa tài khoản ngân hàng
        /// DELETE /api/wallet/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWallet(int id)
        {
            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null)
                return NotFound(new { success = false, message = "Wallet not found" });

            // Kiểm tra quyền truy cập
            var userId = GetCurrentUserId();
            if (wallet.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var deleted = await _walletService.DeleteWalletAsync(id);
            if (!deleted)
                return BadRequest(new { success = false, message = "Failed to delete wallet" });

            return Ok(new { success = true, message = "Wallet deleted successfully" });
        }

        /// <summary>
        /// Đặt tài khoản ngân hàng làm mặc định
        /// POST /api/wallet/{id}/set-default
        /// </summary>
        [HttpPost("{id}/set-default")]
        public async Task<IActionResult> SetDefaultWallet(int id)
        {
            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null)
                return NotFound(new { success = false, message = "Wallet not found" });

            // Kiểm tra quyền truy cập
            var userId = GetCurrentUserId();
            if (wallet.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _walletService.SetDefaultWalletAsync(id, wallet.UserId, wallet.HostId);
            if (!result)
                return BadRequest(new { success = false, message = "Failed to set default wallet" });

            return Ok(new { success = true, message = "Default wallet set successfully" });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }
    }
}

