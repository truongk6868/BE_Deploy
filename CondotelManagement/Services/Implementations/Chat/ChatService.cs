using CondotelManagement.Data;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Chat;
using CondotelManagement.Services.Interfaces.Chat;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services.Implementations.Chat
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _repo;
        private readonly CondotelDbVer1Context _context;

        public ChatService(IChatRepository repo, CondotelDbVer1Context context)
        {
            _repo = repo;
            _context = context;
        }

        // CHUẨN HÓA: Luôn UserAId < UserBId để tránh duplicate
        public async Task<ChatConversation> GetOrCreateDirectConversationAsync(int user1Id, int user2Id)
        {
            if (user1Id == user2Id)
                throw new InvalidOperationException("Không thể tạo conversation với chính mình");

            int aId = Math.Min(user1Id, user2Id);
            int bId = Math.Max(user1Id, user2Id);

            var conv = await _repo.GetDirectConversationAsync(aId, bId);
            if (conv != null) return conv;

            var newConv = new ChatConversation
            {
                ConversationType = "direct",
                UserAId = aId,
                UserBId = bId,
                CreatedAt = DateTime.UtcNow
            };

            return await _repo.CreateConversationAsync(newConv);
        }

        public async Task<int> GetOrCreateDirectConversationIdAsync(int user1Id, int user2Id)
        {
            var conv = await GetOrCreateDirectConversationAsync(user1Id, user2Id);
            return conv.ConversationId;
        }

        public async Task SendDirectMessageAsync(int senderId, int receiverId, string content)
        {
            if (senderId == receiverId)
                throw new InvalidOperationException("Không thể gửi tin nhắn cho chính mình");

            var convId = await GetOrCreateDirectConversationIdAsync(senderId, receiverId);

            await SendMessageAsync(convId, senderId, content);
        }

        public async Task SendMessageAsync(int conversationId, int senderId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var message = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow
            };

            await _repo.AddMessageAsync(message);
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(int conversationId, int take = 100)
            => await _repo.GetMessagesAsync(conversationId, take);

        public async Task<IEnumerable<ChatConversation>> GetMyConversationsAsync(int userId)
            => await _repo.GetUserConversationsAsync(userId);

        public async Task<IEnumerable<ConversationListItem>> GetMyConversationsWithDetailsAsync(int userId)
        {
            var conversations = await _repo.GetUserConversationsAsync(userId);
            var result = new List<ConversationListItem>();

            foreach (var conv in conversations)
            {
                // Bỏ qua nếu conversation lỗi (AId == BId)
                if (conv.UserAId == conv.UserBId || conv.UserAId == null || conv.UserBId == null)
                    continue;

                var messages = await _repo.GetMessagesAsync(conv.ConversationId, 1000);
                var lastMsg = messages
                    .OrderByDescending(m => m.SentAt)
                    .ThenByDescending(m => m.MessageId)
                    .FirstOrDefault();

                var otherUserId = conv.UserAId == userId ? conv.UserBId : conv.UserAId;
                if (otherUserId == userId || otherUserId == null)
                    continue; // Tránh hiển thị chat với chính mình

                var otherUser = conv.UserAId == userId ? conv.UserB : conv.UserA;

                var unreadCount = messages.Count(m => m.SenderId != userId);

                result.Add(new ConversationListItem
                {
                    ConversationId = conv.ConversationId,
                    UserAId = conv.UserAId.Value,
                    UserBId = conv.UserBId.Value,
                    LastMessage = lastMsg,
                    UnreadCount = unreadCount,
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser?.FullName,
                    OtherUserImageUrl = otherUser?.ImageUrl
                });
            }

            return result.OrderByDescending(x => x.LastMessage?.SentAt ?? DateTime.MinValue);
        }

        public async Task AddMessageAsync(ChatMessage message)
            => await _repo.AddMessageAsync(message);

        public async Task<int> GetOtherUserIdInConversationAsync(int conversationId, int currentUserId)
            => await _repo.GetOtherUserIdInConversationAsync(conversationId, currentUserId);
    }
}