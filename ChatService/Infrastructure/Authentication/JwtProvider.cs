using ChatService.Core.Interfaces;
using System.Text.Json.Serialization;

namespace ChatService.Infrastructure.Authentication
{
    internal sealed class JwtProvider : IJwtProvider
    {
        private readonly HttpClient _httpClient;

        public JwtProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(string? IdToken, string? UserId)> GetForCredentialsAsync(string email, string password)
        {
            var request = new
            {
                email,
                password,
                returnSecureToken = true
            };
            var response = await _httpClient.PostAsJsonAsync("", request);
            var authToken = await response.Content.ReadFromJsonAsync<AuthenticationToken>();
            return (authToken?.IdToken, authToken?.LocalId);
        }

        private class AuthenticationToken
        {
            [JsonPropertyName("idToken")]
            public string? IdToken { get; set; }

            [JsonPropertyName("localId")]
            public string? LocalId { get; set; } // Firebase UID
        }
    }
}
