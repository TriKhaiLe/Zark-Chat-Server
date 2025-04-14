using ChatService.Controllers.RequestModels;
using ChatService.Controllers.ResponseModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ChatService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConversationController(
        IConversationRepository conversationRepository,
        IUserRepository userRepository
        ) : ControllerBase
    {
        private readonly IConversationRepository _conversationRepository = conversationRepository;
        private readonly IUserRepository _userRepository = userRepository;

        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create a new conversation")]
        [ProducesResponseType(typeof(CreateConversationResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CreateConversationResponse>> CreateConversation(CreateConversationRequest request)
        {
            if (request.ParticipantIds.Contains(request.CreatorId))
            {
                throw new ArgumentException("CreatorId cannot be in ParticipantIds");
            }

            var conversation = new Conversation
            {
                Type = request.Type,
                Name = request.Type == "Group" ? request.Name : null,
                Participants = request.ParticipantIds.Concat(new[] { request.CreatorId })
                    .Select(userId => new ConversationParticipant
                    {
                        UserId = userId,
                        Role = userId == request.CreatorId && request.Type == "Group" ? "Admin" : "Member",
                        JoinedAt = DateTime.UtcNow
                    }).ToList()
            };

            await _conversationRepository.AddConversationAsync(conversation);
            return Ok(new CreateConversationResponse
            {
                ConversationId = conversation.ConversationId
            });
        }

        [HttpGet("conversations")]
        [SwaggerOperation(Summary = "Get all conversations of an user")]
        [ProducesResponseType(typeof(IEnumerable<ConversationResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConversations()
        {
            var userUid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userUid))
                return Unauthorized("User ID not found in token");

            var user = await _userRepository.GetUserByFirebaseUidAsync(userUid);
            if (user == null)
                return NotFound("User not found");

            var conversations = await _conversationRepository.GetConversationsByUserIdAsync(user.Id);

            var response = conversations.Select(c => new ConversationResponse
            {
                ConversationId = c.ConversationId,
                Type = c.Type,
                Name = c.Name,
                LastMessage = "inconstruction...",
                LastMessageAt = c.LastMessageAt,
                Participants = c.Participants.Select(p => new ParticipantDto
                {
                    UserId = p.UserId,
                    Username = p.User.Username
                }).ToList()
            }).ToList();

            return Ok(response);
        }
    }
}
