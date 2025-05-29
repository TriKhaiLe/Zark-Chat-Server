using ChatService.Core.Entities;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ChatService.Controllers.RequestModels
{
    public class CreateConversationRequest
    {
        [Required]
        public int CreatorId { get; set; }

        [Required, MinLength(1)]
        public List<int> ParticipantIds { get; set; } = new();

        [SwaggerSchema("Type of conversation: 'Private' or 'Group'", Nullable = false)]
        [DefaultValue("Private")]
        [Required, RegularExpression("Private|Group", ErrorMessage = "Type must be 'Private' or 'Group'")]
        public string Type { get; set; } = "Private";

        [MaxLength(100)]
        public string? Name { get; set; }

        public List<EncryptedSessionKeyInfo>? EncryptedSessionKeys { get; set; }
    }
}