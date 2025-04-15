namespace ChatService.Controllers.ResponseModels
{
    internal class ConversationResponse
    {
        public int? ConversationId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }
}