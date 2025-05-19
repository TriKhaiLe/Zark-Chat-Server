using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces;

public interface IEventRepository
{
    Task CreateEventAsync(Event @event);
    Task<List<Event>> GetEventsAsync();
}