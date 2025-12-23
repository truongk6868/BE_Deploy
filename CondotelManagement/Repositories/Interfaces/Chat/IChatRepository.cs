using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces.Chat;

namespace CondotelManagement.Repositories.Interfaces.Chat
{
    public interface IChatRepository
    {
        Task<ChatConversation?> GetDirectConversationAsync(int userAId, int userBId);
        Task<ChatConversation> CreateConversationAsync(ChatConversation conv);
        Task AddMessageAsync(ChatMessage msg);
        Task<IEnumerable<ChatMessage>> GetMessagesAsync(int conversationId, int take = 100);
        Task<IEnumerable<ChatConversation>> GetUserConversationsAsync(int userId);
        Task UpdateConversationLastActivityAsync(int conversationId, int lastMessageId);
        Task IncrementUnreadCountAsync(int conversationId, int senderId); // tăng unread cho người còn lại
        Task<int> GetOtherUserIdInConversationAsync(int conversationId, int currentUserId);
    }
}
