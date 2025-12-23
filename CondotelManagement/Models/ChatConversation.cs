namespace CondotelManagement.Models
{
    public class ChatConversation
    {
        public int ConversationId { get; set; }
        public string? Name { get; set; } // dành cho group chat
        public string ConversationType { get; set; } = "direct"; // direct hoặc group
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Thêm 2 user cho direct chat
        public int? UserAId { get; set; }
        public User UserA { get; set; } = null!; 

        public int? UserBId { get; set; }
        public User UserB { get; set; } = null!; 

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
