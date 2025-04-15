namespace ChatService.Controllers.ResponseModels
{
    internal class SearchResultResponse
    {
        public int ConversationId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string? Avatar { get; set; }
        public bool IsNew { get; set; }
        public int UserId { get; internal set; }
    }
}