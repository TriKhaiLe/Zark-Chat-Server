namespace ChatService.Controllers.ResponseModels
{
    public class MessageResponse
    {
        public int ChatMessageId { get; set; }
        public int ConversationId { get; set; }
        public int UserSendId { get; set; }
        public string? SenderDisplayName { get; set; }
        public string? Message { get; set; }
        public string? MediaLink { get; set; }
        public string Type { get; set; }
        public DateTime SendDate { get; set; }
    }
}