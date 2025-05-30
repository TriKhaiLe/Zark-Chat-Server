namespace ChatService.Controllers.ResponseModels
{
    public class EventDto
    {
        public Guid Id { get; set; }
        
        public int CreatorId { get; set; }
        public string? CreatorDisplayName { get; set; }
        public string? CreatorAvatar { get; set; }
        
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<ParticipantDto> Participants { get; set; } = new();
    }
}

