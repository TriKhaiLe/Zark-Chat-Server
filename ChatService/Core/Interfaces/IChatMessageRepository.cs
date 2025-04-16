using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IChatMessageRepository
    {
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetMessagesByConversationIdAsync(int conversationId, int page, int pageSize);
    }
}
