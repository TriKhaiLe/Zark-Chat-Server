namespace ChatService.Controllers.RequestModels
{
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? DisplayName { get; set; }
    }

}
