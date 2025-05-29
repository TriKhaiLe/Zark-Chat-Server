using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories;

public class EventRepository(ChatDbContext context) : IEventRepository
{
    private readonly ChatDbContext _context = context;
    public async Task CreateEventAsync(Event @event)
    {
        await _context.Events.AddAsync(@event);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Event>> GetEventsAsync()
    {
        var events = await _context.Events
            .Include(e => e.Participants)
            .ToListAsync();
        return events;
    }
}