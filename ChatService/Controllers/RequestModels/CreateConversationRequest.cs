using System.ComponentModel.DataAnnotations;

namespace ChatService.Controllers.RequestModels
{
    public class CreateConversationRequest
    {
        [Required]
        public int CreatorId { get; set; }

        [Required, MinLength(1)]
        public List<int> ParticipantIds { get; set; } = new();

        [Required, RegularExpression("Private|Group", ErrorMessage = "Type must be 'Private' or 'Group'")]
        public string Type { get; set; } = "Private";

        [MaxLength(100)]
        public string? Name { get; set; }
    }
}