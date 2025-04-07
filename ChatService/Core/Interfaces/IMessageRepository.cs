using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IMessageRepository
    {
        Task<Message> AddMessageAsync(Message message);
        Task<List<Message>> GetMessagesBetweenUsersAsync(int user1Id, int user2Id, int pageNumber, int pageSize);
    }
}
