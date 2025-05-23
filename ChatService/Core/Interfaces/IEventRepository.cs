using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces;

public interface IEventRepository
{
    Task CreateEventAsync(Event @event);
    Task<List<Event>> GetEventsByUserIdAsync(int userId);
    Task<Event?> GetEventByIdAsync(Guid id);
    Task DeleteEventAsync(Guid id);
    Task UpdateEventAsync(Guid id, Event @event);
}