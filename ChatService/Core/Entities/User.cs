namespace ChatService.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public string FirebaseUid { get; set; } = string.Empty;
        public ICollection<Message> SentMessages { get; set; } = [];
        public ICollection<Message> ReceivedMessages { get; set; } = [];
    }
}
