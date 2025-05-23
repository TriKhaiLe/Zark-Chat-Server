using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;

namespace ChatService.Mapper;

public static class EventMapper
{
    public static Event ToEntity(this EventRequest? request)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            CreatorId = request.CreatorId,
            Title = request.Title,
            Description = request.Description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Participants = request.Participants.Select(userId => new Participant
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = "Pending"
            }).ToList()
        };
    }
}