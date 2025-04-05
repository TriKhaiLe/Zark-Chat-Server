using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByFirebaseUid(string firebaseUid);
        Task<User> GetUserByIdAsync(int userId);
        Task AddConnectionIdAsync(int userId, string connectionId);
        Task RemoveConnectionIdAsync(int userId, string connectionId);
    }
}
