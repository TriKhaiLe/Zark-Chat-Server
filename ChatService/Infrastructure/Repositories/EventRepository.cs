using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
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

    public Task<List<Event>> GetEventsByIdAsync(int userId)
    {
        var events = _context.Events.Where(e => e.CreatorId == userId).ToList();

        foreach (var eventItem in events)
        {
            eventItem.Participants = _context.Participants
                .Where(p => p.EventId == eventItem.Id)
                .ToList();
        }

        return Task.FromResult(events);
    }

    public async Task<List<Participant>> GetParticipantsByEventIdAsync(int userId, string eventId)
    {
        var eventGuid = Guid.TryParse(eventId, out var parsedId)
            ? parsedId
            : throw new ArgumentException("Invalid event ID");
        var paticipants = await _context.Participants
            .Where(p => p.EventId == eventGuid)
            .ToListAsync();
        return paticipants;
    }

    public async Task DeleteEventAsync(int userId, string eventId)
    {
        var eventGuid = Guid.TryParse(eventId, out var parsedId)
            ? parsedId
            : throw new ArgumentException("Invalid event ID");

        var eventToDelete = await _context.Events
            .FirstOrDefaultAsync(e => e.CreatorId == userId && e.Id == eventGuid);

        if (eventToDelete == null)
        {
            throw new KeyNotFoundException("Event not found or not owned by user.");
        }

        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(Event @event)
    {
        var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.Id == @event.Id);

        if (eventToUpdate == null)
        {
            throw new KeyNotFoundException("Event not found.");
        }

        _context.Events.Update(eventToUpdate);
        await _context.SaveChangesAsync();
    }
}