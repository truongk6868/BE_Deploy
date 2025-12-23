using CondotelManagement.DTOs.Wallet;
using CondotelManagement.Services.Interfaces.Wallet;
using CondotelManagement.Services.Interfaces.Host;
using CondotelManagement.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CondotelManagement.Services.Interfaces;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/wallet")]
    [Authorize(Roles = "Host")]
    public class HostWalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IHostService _hostService;

        public HostWalletController(IWalletService walletService, IHostService hostService)
        {
            _walletService = walletService;
            _hostService = hostService;
        }

        /// <summary>
        /// Lấy danh sách tài khoản ngân hàng của host
        /// GET /api/host/wallet
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyWallets()
        {
            var hostId = GetHostId();
            var wallets = await _walletService.GetWalletsByHostIdAsync(hostId);
            
            return Ok(new
            {
                success = true,
                data = wallets,
                total = wallets.Count()
            });
        }

        /// <summary>
        /// Tạo tài khoản ngân hàng mới cho host
        /// POST /api/host/wallet
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromBody] WalletCreateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
                    return Unauthorized(new { success = false, message = "Host not found" });

                // Chỉ set HostId, không set UserId (vì CHECK constraint CK_Wallet_OneOwner yêu cầu chỉ một trong hai)
                dto.HostId = host.HostId;
                dto.UserId = null; // Không set UserId cho wallet của host

                var created = await _walletService.CreateWalletAsync(dto);
                return CreatedAtAction(nameof(GetMyWallets), null, new
                {
                    success = true,
                    message = "Wallet created successfully",
                    data = created
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Xử lý lỗi database constraint
                var errorMessage = "Failed to create wallet. ";
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.Message.Contains("UNIQUE") || ex.InnerException.Message.Contains("duplicate"))
                        errorMessage += "A wallet with this information already exists.";
                    else
                        errorMessage += ex.InnerException.Message;
                }
                else
                    errorMessage += ex.Message;

                return StatusCode(500, new { success = false, message = errorMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while creating wallet", error = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật tài khoản ngân hàng
        /// PUT /api/host/wallet/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWallet(int id, [FromBody] WalletUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null)
                return NotFound(new { success = false, message = "Wallet not found" });

            var hostId = GetHostId();
            if (!wallet.HostId.HasValue || wallet.HostId.Value != hostId)
                return StatusCode(403, new { success = false, message = "Wallet does not belong to this host" });

            var updated = await _walletService.UpdateWalletAsync(id, dto);
            if (!updated)
                return BadRequest(new { success = false, message = "Failed to update wallet" });

            return Ok(new { success = true, message = "Wallet updated successfully" });
        }

        /// <summary>
        /// Xóa tài khoản ngân hàng
        /// DELETE /api/host/wallet/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWallet(int id)
        {
            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null)
                return NotFound(new { success = false, message = "Wallet not found" });

            var hostId = GetHostId();
            if (!wallet.HostId.HasValue || wallet.HostId.Value != hostId)
                return StatusCode(403, new { success = false, message = "Wallet does not belong to this host" });

            var deleted = await _walletService.DeleteWalletAsync(id);
            if (!deleted)
                return BadRequest(new { success = false, message = "Failed to delete wallet" });

            return Ok(new { success = true, message = "Wallet deleted successfully" });
        }

        /// <summary>
        /// Đặt tài khoản ngân hàng làm mặc định
        /// POST /api/host/wallet/{id}/set-default
        /// </summary>
        [HttpPost("{id}/set-default")]
        public async Task<IActionResult> SetDefaultWallet(int id)
        {
            try
            {
                var wallet = await _walletService.GetWalletByIdAsync(id);
                if (wallet == null)
                    return NotFound(new { success = false, message = "Wallet not found" });

                var hostId = GetHostId();
                
                // Kiểm tra wallet có thuộc về host này không
                if (!wallet.HostId.HasValue || wallet.HostId.Value != hostId)
                    return StatusCode(403, new { success = false, message = "Wallet does not belong to this host" });

                // Kiểm tra wallet có đang là default chưa
                if (wallet.IsDefault)
                    return BadRequest(new { success = false, message = "Wallet is already set as default" });

                // Chỉ truyền hostId (không truyền userId vì wallet của host chỉ có HostId)
                var result = await _walletService.SetDefaultWalletAsync(id, null, hostId);
                if (!result)
                {
                    // Có thể do: wallet không thuộc về host, hoặc có lỗi khi save
                    return BadRequest(new { 
                        success = false, 
                        message = "Failed to set default wallet. Please ensure the wallet belongs to this host and try again." 
                    });
                }

                return Ok(new { success = true, message = "Default wallet set successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while setting default wallet", error = ex.Message });
            }
        }

        private int GetHostId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            var host = _hostService.GetByUserId(userId);
            if (host == null)
                throw new UnauthorizedAccessException("Host not found");

            return host.HostId;
        }
    }
}

