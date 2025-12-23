using CondotelManagement.Data;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Chat;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Repositories.Implementations.Chat
{
    public class ChatRepository : IChatRepository
    {
        private readonly CondotelDbVer1Context _ctx;

        public ChatRepository(CondotelDbVer1Context ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Tìm conversation direct giữa 2 user, bất kể thứ tự UserAId/UserBId.
        /// </summary>
        public async Task<ChatConversation?> GetDirectConversationAsync(int userAId, int userBId)
        {
            return await _ctx.ChatConversations
                .FirstOrDefaultAsync(c =>
                    c.ConversationType == "direct" &&
                    (
                        (c.UserAId == userAId && c.UserBId == userBId) ||
                        (c.UserAId == userBId && c.UserBId == userAId)
                    )
                );
        }

        /// <summary>
        /// Tạo conversation mới và lưu vào DB.
        /// </summary>
        public async Task<ChatConversation> CreateConversationAsync(ChatConversation conv)
        {
            await _ctx.ChatConversations.AddAsync(conv);
            await _ctx.SaveChangesAsync();
            return conv;
        }

        /// <summary>
        /// Thêm tin nhắn mới và lưu ngay vào DB (có SaveChanges).
        /// </summary>
        public async Task AddMessageAsync(ChatMessage msg)
        {
            await _ctx.ChatMessages.AddAsync(msg);
            await _ctx.SaveChangesAsync(); // Quan trọng: đảm bảo tin nhắn được lưu ngay
        }

        /// <summary>
        /// Lấy tin nhắn của conversation, sắp xếp cũ → mới (dễ hiển thị ở client).
        /// </summary>
        public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(int conversationId, int take = 100)
        {
            return await _ctx.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.MessageId) // Nếu cùng giây thì theo ID tăng dần
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tất cả conversation mà user tham gia (Include thông tin UserA và UserB).
        /// </summary>
        public async Task<IEnumerable<ChatConversation>> GetUserConversationsAsync(int userId)
        {
            return await _ctx.ChatConversations
                .Include(c => c.UserA)
                .Include(c => c.UserB)
                .Where(c => c.UserAId == userId || c.UserBId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy ID của người kia trong conversation.
        /// Trả về 0 nếu không tìm thấy (an toàn cho SignalR).
        /// </summary>
        public async Task<int> GetOtherUserIdInConversationAsync(int conversationId, int currentUserId)
        {
            var conv = await _ctx.ChatConversations
                .Where(c => c.ConversationId == conversationId)
                .Select(c => new { c.UserAId, c.UserBId })
                .FirstOrDefaultAsync();

            if (conv == null) return 0;

            // Lấy người kia một cách an toàn
            return conv.UserAId == currentUserId
                ? (conv.UserBId ?? 0)
                : (conv.UserAId ?? 0);
        }

        /// <summary>
        /// (Tùy chọn) Lấy conversation theo ID – hữu ích cho debug hoặc mở rộng sau này.
        /// </summary>
        public async Task<ChatConversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _ctx.ChatConversations
                .Include(c => c.UserA)
                .Include(c => c.UserB)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        // ==================================================================
        // CÁC METHOD DƯ THỪA – KHÔNG CẦN THIẾT VÌ UNREAD COUNT ĐƯỢC TÍNH REAL-TIME
        // ==================================================================

        /// <summary>
        /// Không dùng nữa vì unreadCount được tính trong service (GetMyConversationsWithDetailsAsync).
        /// Để lại để tương thích interface, nhưng không làm gì.
        /// </summary>
        public Task UpdateConversationLastActivityAsync(int conversationId, int lastMessageId)
        {
            // Không cần cập nhật cột LastActivity riêng → tính theo last message
            return Task.CompletedTask;
        }

        /// <summary>
        /// Không dùng nữa vì không có bảng ConversationParticipants.
        /// UnreadCount được tính trực tiếp từ số tin nhắn chưa đọc trong service.
        /// </summary>
        public Task IncrementUnreadCountAsync(int conversationId, int senderId)
        {
            // Không làm gì cả – unread sẽ được tính lại khi load conversations
            return Task.CompletedTask;
        }
    }
}