namespace ChatService.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Task<string> RegisterAsync(string email, string password);
    }
}
