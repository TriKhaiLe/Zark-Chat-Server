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

    public async Task AddParticipantAsync(Participant? participant)
    {
        if (participant == null)
        {
            throw new NullReferenceException("Participant not found.");
        }

        var host = await _context.Events.FirstOrDefaultAsync(e => e.Id == participant.EventId);

        if (host != null && host.CreatorId == participant.UserId)
        {
            throw new InvalidOperationException("Participant already owned by user.");
        }

        var participantExist = await _context.Participants.FirstOrDefaultAsync(p =>
            p.UserId == participant.UserId && p.EventId == participant.EventId);

        if (participantExist != null)
        {
            throw new KeyNotFoundException("Participant have joined this event.");
        }

        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveParticipantAsync(Guid eventId, int userId)
    {
        var participants = await _context.Participants.Where(p => p.EventId == eventId).ToListAsync();
        var participantToDelete = participants.FirstOrDefault(p => p.UserId == userId);
        if (participantToDelete == null)
        {
            throw new KeyNotFoundException("Participant not found.");
        }

        _context.Participants.Remove(participantToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task SetStatusInvitation(Guid eventId, int userId, string status)
    {
        var participants = await _context.Participants.Where(p => p.EventId == eventId).ToListAsync();
        var participantToAcceptInvitation = participants.FirstOrDefault(p => p.UserId == userId);


        if (participantToAcceptInvitation == null)
        {
            throw new KeyNotFoundException("Participant not found.");
        }

        if (participantToAcceptInvitation.Status == status)
        {
            throw new InvalidOperationException($"Invitation already {status} by user.");
        }

        participantToAcceptInvitation.Status = status;
        await _context.SaveChangesAsync();
    }

    public async Task<List<Participant>> GetParticipantInvitationByStatus(Guid eventId, string status)
    {
        var participants = await _context.Participants.Where(p => p.EventId == eventId && p.Status == status).ToListAsync();
        if (!participants.Any())
        {
            throw new NullReferenceException("No accepted participant in this event.");
        }

        return participants;
    }

    public async Task MarkEventAsDone(Guid eventId)
    {
        var eventFind = await _context.Events.FindAsync(eventId);
        if (eventFind == null )
        {
            throw new KeyNotFoundException("Event not found.");
        }

        eventFind.Status = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<Event>> GetEventsToNotifyAsync(DateTime currentTime)
    {
        var from = currentTime.AddSeconds(-30);
        var to = currentTime.AddSeconds(30);

        return await _context.Events
            .Include(e => e.Participants)
            .Where(e => !e.IsNotification && e.NotificationTime >= from && e.NotificationTime <= to)
            .ToListAsync();
    }


    public async Task MarkNotificationSent(Guid eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev != null)
        {
            ev.IsNotification = true;
            await _context.SaveChangesAsync();
        }
    }
}