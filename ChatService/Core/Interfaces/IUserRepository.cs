using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByFirebaseUidAsync(string firebaseUid);
        Task AddConnectionIdAsync(int userId, string connectionId);
        Task RemoveConnectionIdAsync(int userId, string connectionId);
        Task<List<User>> GetContactsAsync(int userId);
        Task<List<UserConnection>> GetConnectionsByUserIdsAsync(List<int> userIds);
        Task AddUserAsync(User newUser);
        Task<List<User>> GetUsersByNameAsync(string query);
        Task<List<User>> GetUsersByIdsAsync(List<int> allUserIds);
    }
}
