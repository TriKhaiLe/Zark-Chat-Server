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

    public async Task<List<Event>> GetEventsByUserIdAsync(int userId)
    {
        var events = await _context.Events
            .Where(e => e.CreatorId == userId)
            .Include(e => e.Participants)
            .ToListAsync();

        return events;
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        var eventItem = await _context.Events.Where(e => e.Id == id).Include(e => e.Participants).FirstOrDefaultAsync();
        return eventItem;
    }

    public async Task DeleteEventAsync(Guid id)
    {
        var eventToDelete = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventToDelete == null)
        {
            throw new KeyNotFoundException("Event not found or not owned by user.");
        }

        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(Guid id, Event @event)
    {
        var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);

        if (eventToUpdate == null)
        {
            throw new KeyNotFoundException("Event not found.");
        }

        eventToUpdate.Title = @event.Title;
        eventToUpdate.Description = @event.Description;
        eventToUpdate.StartTime = @event.StartTime;
        eventToUpdate.EndTime = @event.EndTime;

        _context.Events.Update(eventToUpdate);
        await _context.SaveChangesAsync();
    }
}