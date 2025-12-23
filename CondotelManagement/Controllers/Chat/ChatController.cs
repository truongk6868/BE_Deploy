using CondotelManagement.Services.Interfaces.Chat;
using CondotelManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Controllers.Chat
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly CondotelDbVer1Context _context;

        public ChatController(IChatService chatService, CondotelDbVer1Context context)
        {
            _chatService = chatService;
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("nameid")
                     ?? User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");

            if (claim == null || !int.TryParse(claim.Value, out int userId))
                throw new UnauthorizedAccessException("Không tìm thấy user ID trong token");

            return userId;
        }

        /// <summary>
        /// Lấy danh sách tất cả hội thoại của user hiện tại
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var conversations = await _chatService.GetMyConversationsWithDetailsAsync(userId);

                var result = conversations.Select(c => new
                {
                    conversationId = c.ConversationId,
                    userAId = c.UserAId,
                    userBId = c.UserBId,
                    otherUser = c.OtherUserId.HasValue ? new
                    {
                        userId = c.OtherUserId.Value,
                        fullName = c.OtherUserName,
                        imageUrl = c.OtherUserImageUrl
                    } : null,
                    lastMessage = c.LastMessage != null ? new
                    {
                        content = c.LastMessage.Content,
                        sentAt = c.LastMessage.SentAt,
                        senderId = c.LastMessage.SenderId
                    } : null,
                    unreadCount = c.UnreadCount
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy danh sách hội thoại", message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy tin nhắn của một hội thoại cụ thể
        /// </summary>
        [HttpGet("messages/{conversationId}")]
        public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int take = 100)
        {
            try
            {
                var userId = GetCurrentUserId(); // Dùng để check quyền nếu cần (tùy chọn)

                var msgs = await _chatService.GetMessagesAsync(conversationId, take);

                // Lấy thông tin người gửi (fullName, imageUrl)
                var senderIds = msgs.Select(m => m.SenderId).Distinct().ToList();
                var users = await _context.Users
                    .Where(u => senderIds.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => new { u.FullName, u.ImageUrl });

                var result = msgs.Select(m => new
                {
                    m.MessageId,
                    m.ConversationId,
                    m.SenderId,
                    sender = users.ContainsKey(m.SenderId) ? new
                    {
                        userId = m.SenderId,
                        fullName = users[m.SenderId].FullName,
                        imageUrl = users[m.SenderId].ImageUrl
                    } : null,
                    m.Content,
                    sentAt = DateTime.SpecifyKind(m.SentAt, DateTimeKind.Utc) // Frontend sẽ tự convert múi giờ
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy tin nhắn", message = ex.Message });
            }
        }

        /// <summary>
        /// Gửi tin nhắn trực tiếp đến một user khác (dùng từ frontend nếu cần)
        /// </summary>
        [HttpPost("messages/send-direct")]
        public async Task<IActionResult> SendDirectMessage([FromBody] DirectMessageRequest request)
        {
            try
            {
                var senderId = GetCurrentUserId();

                // Bảo mật: Không cho gửi từ ID khác
                if (senderId != request.SenderId)
                    return BadRequest(new { error = "SenderId không hợp lệ" });

                await _chatService.SendDirectMessageAsync(senderId, request.ReceiverId, request.Content);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi gửi tin nhắn", message = ex.Message });
            }
        }

        public class DirectMessageRequest
        {
            public int SenderId { get; set; }
            public int ReceiverId { get; set; }
            public string Content { get; set; } = string.Empty;
        }

        /// <summary>
        /// MỚI: Gửi tin nhắn đến chủ condotel (dùng từ trang chi tiết condotel)
        /// Trả về conversationId để frontend mở chat chính xác
        /// </summary>
        [HttpPost("messages/send-to-host")]
        public async Task<IActionResult> SendToCondotelHost([FromBody] SendMessageToCondotelHostRequest request)
        {
            try
            {
                var senderId = GetCurrentUserId();

                // Lấy condotel và JOIN với bảng Host để lấy UserID thật của host
                var condotel = await _context.Condotels
                    .AsNoTracking()
                    .Include(c => c.Host)  // Đảm bảo có navigation property Host trong model Condotel
                    .FirstOrDefaultAsync(c => c.CondotelId == request.CondotelId);

                if (condotel == null)
                    return NotFound(new { error = "Condotel không tồn tại" });

                // LẤY USERID THẬT CỦA HOST TỪ BẢNG HOST
                var hostUserId = condotel.Host?.UserId;

                if (hostUserId == null)
                    return NotFound(new { error = "Host của condotel không tồn tại" });

                if (hostUserId == senderId)
                    return BadRequest(new { error = "Không thể chat với chính mình" });

                // Dùng hostUserId (UserID thật) để tạo conversation và gửi tin
                var conversationId = await _chatService.GetOrCreateDirectConversationIdAsync(senderId, hostUserId.Value);
                await _chatService.SendMessageAsync(conversationId, senderId, request.Content);

                return Ok(new { conversationId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi mở chat với host", message = ex.Message });
            }
        }

        public class SendMessageToCondotelHostRequest
        {
            public int CondotelId { get; set; }
            public string Content { get; set; } = string.Empty;
        }
    }
}//commit again 