using ChatService.Controllers.ResponseModels;
using ChatService.Core.Entities;

namespace ChatService.Mapper;

public class ParticipantMapper
{
    public static ParticipantDto MapToDto(Participant participant)
    {
        return new ParticipantDto
        {
            Id = participant.UserId,
            Status = participant.Status,
            DisplayName = participant.User?.DisplayName ?? "",
            Avatar = participant.User?.AvatarUrl ?? "",
        };
    }
}