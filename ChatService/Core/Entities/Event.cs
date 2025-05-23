namespace ChatService.Core.Entities;

public class Event
{
    public Guid Id { get; set; }
    public int CreatorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<Participant> Participants { get; set; } = new();
}

public class Participant
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; }
    
    public Guid EventId { get; set; }
}