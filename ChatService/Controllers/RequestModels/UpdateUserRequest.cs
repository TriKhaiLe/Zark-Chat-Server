namespace ChatService.Controllers.RequestModels
{
    public class UpdateUserRequest
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? PublicKey { get; set; }
    }
}
