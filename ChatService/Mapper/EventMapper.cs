using ChatService.Controllers.RequestModels;
using ChatService.Controllers.ResponseModels;
using ChatService.Core.Entities;

namespace ChatService.Mapper;

public static class EventMapper
{
    public static Event ToEntity(this EventRequest? request)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            CreatorId = request!.CreatorId,
            Title = request.Title,
            Description = request.Description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Participants = request.Participants.Select(userId => new Participant
            {
                UserId = userId,
                Status = "Pending"
            }).ToList()
        };
    }

    public static EventDto ToEventDto(Event? @event)
    {
        return new EventDto
        {
            Id = @event!.Id,

            CreatorId = @event.CreatorId,
            CreatorDisplayName = @event.Creator.DisplayName,
            CreatorAvatar = @event.Creator.AvatarUrl,

            Title = @event.Title,
            Description = @event.Description,
            StartDate = @event.StartTime,
            EndDate = @event.EndTime,

            Participants = @event.Participants?.Select(ParticipantMapper.MapToDto).ToList() ?? new List<ParticipantDto>()
        };
    }

}