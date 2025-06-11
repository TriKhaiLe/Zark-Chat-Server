namespace ChatService.Controllers.RequestModels
{
    public class EventRequest
    {
        public int CreatorId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<int> Participants { get; set; } = new();
        public DateTime? NotificationTime { get; set; }
    }

    public class EventUpdateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}