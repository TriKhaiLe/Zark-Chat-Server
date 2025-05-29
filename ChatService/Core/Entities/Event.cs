namespace ChatService.Core.Entities;

public class Event
{
    public Guid Id { get; set; }
    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ICollection<Participant>? Participants { get; set; }
}

public class Participant
{
    public int UserId { get; set; }
    public Guid EventId { get; set; }
    public string Status { get; set; }
    public Event Event { get; set; }
    public User User { get; set; } = null!;
    
}