using ChatService.Controllers;

namespace ChatService.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Task<string> RegisterAsync(string email, string password);
        Task<string> GetUidByEmailAsync(string email);
        Task<List<FirebaseDto>> SyncEmailAsync();
    }
}
