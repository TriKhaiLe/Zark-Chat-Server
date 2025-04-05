﻿namespace ChatService.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirebaseUid { get; set; } = string.Empty;
        public ICollection<Message> SentMessages { get; set; } = [];
        public ICollection<Message> ReceivedMessages { get; set; } = [];
        public List<UserConnection> Connections { get; set; } = new List<UserConnection>();
    }

    public class UserConnection
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ConnectionId { get; set; }
        public User User { get; set; } // Navigation property
    }
}
