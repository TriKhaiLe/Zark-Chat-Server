using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetConversationByIdAsync(int conversationId);
        Task UpdateLastMessageTimeAsync(int conversationId, DateTime lastMessageAt);
        Task AddConversationAsync(Conversation conversation);
        Task<List<Conversation>> GetConversationsByUserIdAsync(int userId);
    }
}
