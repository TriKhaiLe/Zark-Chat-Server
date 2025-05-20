namespace ChatService.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirebaseUid { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public List<UserConnection> Connections { get; set; } = new List<UserConnection>();
        public List<UserDevice> Devices { get; set; } = new List<UserDevice>();
        public bool IsValidAccount { get; set; } = false;
    }

    public class UserConnection
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ConnectionId { get; set; }
        public User User { get; set; } // Navigation property
    }

    public class UserDevice
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? FcmToken { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
