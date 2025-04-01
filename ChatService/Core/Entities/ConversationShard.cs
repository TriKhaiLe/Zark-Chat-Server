namespace ChatService.Core.Entities
{
    public class ConversationShard
    {
        public string ConversationId { get; set; }
        public int ShardId { get; set; }
        public string ShardConnectionString { get; set; }
    }
}