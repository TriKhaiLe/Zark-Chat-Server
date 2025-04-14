namespace ChatService.Controllers.RequestModels
{
    public class MessageRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}