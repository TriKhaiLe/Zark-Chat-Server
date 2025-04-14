using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IMessageRepository
    {
        Task<Message> AddMessageAsync(Message message);
        Task<List<Message>> GetMessagesBetweenUsersAsync(int user1Id, int user2Id, int pageNumber, int pageSize);
    }

    public interface IChatMessageRepository
    {
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetMessagesByConversationIdAsync(int conversationId, int page, int pageSize);
    }
}
