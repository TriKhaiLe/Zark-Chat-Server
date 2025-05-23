using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces;

public interface IEventRepository
{
    Task CreateEventAsync(Event @event);
    Task<List<Event>> GetEventsByIdAsync(int userId);
    
    Task<List<Participant>> GetParticipantsByEventIdAsync(int userId, string eventId);
    Task DeleteEventAsync(int userId, string eventId);
    Task UpdateEventAsync(Event @event);
}