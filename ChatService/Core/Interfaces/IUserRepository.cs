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
        Task UpdateValidationAccount(string email);
        Task UpdateEmailUserAsync(string email, string firebaseUid);
        Task AddFcmTokenAsync(int userId, string fcmToken);
        Task<List<string?>> GetFcmTokensByUserIdsAsync(List<int> userIds);
        Task RemoveAllFcmTokensAsync(int userId);
        Task UpdateUserAsync(int userId, string? displayName = null, string? avatarUrl = null, string? publicKey = null);
    }
}
