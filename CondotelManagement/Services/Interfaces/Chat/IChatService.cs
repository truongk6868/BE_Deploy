using CondotelManagement.Models;

namespace CondotelManagement.Services.Interfaces.Chat
{
    public interface IChatService
    {
        Task<ChatConversation> GetOrCreateDirectConversationAsync(int user1Id, int user2Id);
        Task<int> GetOrCreateDirectConversationIdAsync(int user1Id, int user2Id); // MỚI: Trả về chỉ ID

        Task SendMessageAsync(int conversationId, int senderId, string content);
        Task SendDirectMessageAsync(int senderId, int receiverId, string content);

        Task<IEnumerable<ChatMessage>> GetMessagesAsync(int conversationId, int take = 100);
        Task<IEnumerable<ChatConversation>> GetMyConversationsAsync(int userId);
        Task<IEnumerable<ConversationListItem>> GetMyConversationsWithDetailsAsync(int userId);

        Task<int> GetOtherUserIdInConversationAsync(int conversationId, int currentUserId);
        Task AddMessageAsync(ChatMessage message);
    }

    public class ConversationListItem
    {
        public int ConversationId { get; set; }
        public int UserAId { get; set; }
        public int UserBId { get; set; }
        public ChatMessage? LastMessage { get; set; }
        public int UnreadCount { get; set; }

        public int? OtherUserId { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserImageUrl { get; set; }
    }
}