namespace ChatService.Core.Entities
{
    public class Conversation
    {
        public int ConversationId { get; set; }
        public string Type { get; set; } = "Private";
        public string Name { get; set; } = string.Empty;
        public DateTime? LastMessageAt { get; set; }
        public virtual ICollection<ConversationParticipant> Participants { get; set; } = [];
        public virtual ICollection<ChatMessage> Messages { get; set; } = [];
        public List<EncryptedSessionKeyInfo> EncryptedSessionKeys { get; set; } = new List<EncryptedSessionKeyInfo>();
    }

    public class ConversationParticipant
    {
        public int ConversationId { get; set; }
        public virtual Conversation? Conversation { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public string Role { get; set; } = "Member";
        public DateTime? JoinedAt { get; set; }
    }

    public class ChatMessage
    {
        public int ChatMessageId { get; set; }
        public int ConversationId { get; set; }
        public virtual Conversation Conversation { get; set; }
        public int UserSendId { get; set; }
        public virtual User Sender { get; set; }
        public DateTime SendDate { get; set; }
        public string? Message { get; set; }
        public string? MediaLink { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public virtual ICollection<MessageReadStatus> ReadStatuses { get; set; }
        public string? EncryptedSessionKey { get; set; }
    }

    public class MessageReadStatus
    {
        public int ChatMessageId { get; set; }
        public int UserId { get; set; }
        public DateTime? ReadAt { get; set; }
        public virtual ChatMessage Message { get; set; }
        public virtual User User { get; set; }
    }

    public class EncryptedSessionKeyInfo
    {
        public int UserId { get; set; }
        public string EncryptedSessionKey { get; set; } = string.Empty;
    }
}