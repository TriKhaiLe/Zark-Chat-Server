namespace ChatService.Core.Interfaces
{
    public interface IJwtProvider
    {
        Task<(string? IdToken, string? UserId)> GetForCredentialsAsync(string email, string password);
    }
}
