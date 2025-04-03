namespace ChatService.Core.Interfaces
{
    public interface IJwtProvider
    {
        Task<string?> GetForCredentialsAsync(string email, string password);
    }
}
