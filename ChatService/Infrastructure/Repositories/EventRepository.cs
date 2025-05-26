using ChatService.Controllers.RequestModels;
using ChatService.Controllers.ResponseModels;
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
        return await _context.Events
            .Where(e => e.CreatorId == userId)
            .Include(e => e.Creator!)
            .Include(e => e.Participants).ThenInclude(p => p.User)
            .AsNoTracking()
            .ToListAsync();
    }
    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        var eventItem = await _context.Events
            .Where(e => e.Id == id)
            .Include(e => e.Creator)
            .Include(e => e.Participants)
            .FirstOrDefaultAsync();
        return eventItem;
    }

    public async Task DeleteEventAsync(Guid id)
    {
        var participants = await _context.Participants.Where(p => p.EventId == id).ToListAsync();
        _context.Participants.RemoveRange(participants);

        var eventToDelete = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventToDelete == null)
        {
            throw new KeyNotFoundException("Event not found or not owned by user.");
        }

        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(Guid id, EventUpdateRequest @event)
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