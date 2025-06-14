﻿using ChatService.Controllers.RequestModels;
using ChatService.Controllers.ResponseModels;
using ChatService.Core.Entities;

namespace ChatService.Core.Interfaces;

public interface IEventRepository
{
    Task CreateEventAsync(Event @event);
    Task<List<Event>> GetEventsByUserIdAsync(int userId);
    Task<Event?> GetEventByIdAsync(Guid id);
    Task DeleteEventAsync(Guid id);
    Task UpdateEventAsync(Guid id, EventUpdateRequest @event);
    Task AddParticipantAsync(Participant? participant);
    Task RemoveParticipantAsync(Guid eventId, int userId);
    Task SetStatusInvitation(Guid eventId, int userId, string status);
    Task<List<Participant>> GetParticipantInvitationByStatus(Guid eventId, string status);
    Task MarkEventAsDone(Guid eventId);
    Task<List<Event>> GetEventsToNotifyAsync(DateTime currentTime);
    Task MarkNotificationSent(Guid eventId);
}