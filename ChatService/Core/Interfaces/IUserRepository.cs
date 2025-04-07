using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByFirebaseUid(string firebaseUid);
        Task AddConnectionIdAsync(int userId, string connectionId);
        Task RemoveConnectionIdAsync(int userId, string connectionId);
        Task<List<User>> GetContactsAsync(int userId);
    }
}
