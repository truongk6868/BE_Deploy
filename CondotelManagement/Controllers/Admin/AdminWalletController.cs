using CondotelManagement.DTOs.Wallet;
using CondotelManagement.Services.Interfaces.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/wallets")]
    [Authorize(Roles = "Admin")]
    public class AdminWalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public AdminWalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Lấy tất cả wallets
        /// GET /api/admin/wallets?userId=1&hostId=2
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllWallets([FromQuery] int? userId = null, [FromQuery] int? hostId = null)
        {
            IEnumerable<WalletDTO> wallets;

            if (userId.HasValue)
            {
                wallets = await _walletService.GetWalletsByUserIdAsync(userId.Value);
            }
            else if (hostId.HasValue)
            {
                wallets = await _walletService.GetWalletsByHostIdAsync(hostId.Value);
            }
            else
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp userId hoặc hostId" });
            }

            return Ok(new
            {
                success = true,
                data = wallets,
                total = wallets.Count()
            });
        }

        /// <summary>
        /// Lấy wallet theo ID
        /// GET /api/admin/wallets/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWalletById(int id)
        {
            var wallet = await _walletService.GetWalletByIdAsync(id);
            if (wallet == null)
                return NotFound(new { success = false, message = "Không tìm thấy ví" });

            return Ok(new { success = true, data = wallet });
        }

        /// <summary>
        /// Tạo wallet mới
        /// POST /api/admin/wallets
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromBody] WalletCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });

            var created = await _walletService.CreateWalletAsync(dto);
            return CreatedAtAction(nameof(GetWalletById), new { id = created.WalletId }, new
            {
                success = true,
                message = "Ví đã được tạo thành công",
                data = created
            });
        }

        /// <summary>
        /// Cập nhật wallet
        /// PUT /api/admin/wallets/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWallet(int id, [FromBody] WalletUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });

            var updated = await _walletService.UpdateWalletAsync(id, dto);
            if (!updated)
                return NotFound(new { success = false, message = "Không tìm thấy ví" });

            return Ok(new { success = true, message = "Ví đã được cập nhật thành công" });
        }

        /// <summary>
        /// Xóa wallet
        /// DELETE /api/admin/wallets/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWallet(int id)
        {
            var deleted = await _walletService.DeleteWalletAsync(id);
            if (!deleted)
                return NotFound(new { success = false, message = "Không tìm thấy ví" });

            return Ok(new { success = true, message = "Ví đã được xóa thành công" });
        }
    }
}

