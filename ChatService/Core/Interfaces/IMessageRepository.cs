using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IMessageRepository
    {
        Task AddMessageAsync(string conversationId, Message message);
        Task<List<Message>> GetMessagesAsync(string conversationId, int limit = 50);
    }
}
