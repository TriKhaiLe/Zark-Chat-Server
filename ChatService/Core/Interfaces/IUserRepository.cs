using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByFirebaseUid(string firebaseUid);
    }
}
