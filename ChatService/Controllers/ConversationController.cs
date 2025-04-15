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

            string conversationName = string.Empty;
            if (request.Type == "Private")
            {
                var otherUserId = request.ParticipantIds.FirstOrDefault(id => id != request.CreatorId);
                if (otherUserId == 0)
                    throw new ArgumentException("Private conversation must have one other participant");

                var otherUser = await _userRepository.GetUserByIdAsync(otherUserId);
                if (otherUser == null)
                    throw new ArgumentException("The other participant does not exist");

                conversationName = otherUser.DisplayName;
            }
            else if (request.Type == "Group")
            {
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    conversationName = request.Name;
                }
                else
                {
                    // Lấy danh sách userId bao gồm creator + participant
                    var allUserIds = request.ParticipantIds.Append(request.CreatorId).ToList();
                    var users = await _userRepository.GetUsersByIdsAsync(allUserIds);
                    conversationName = string.Join(", ", users.Select(u => u.DisplayName));
                }
            }

            var conversation = new Conversation
            {
                Type = request.Type,
                Name = conversationName,
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
                    DisplayName = p.User.DisplayName
                }).ToList()
            }).ToList();

            return Ok(response);
        }

        [HttpGet("private-conversation")]
        [SwaggerOperation(Summary = "Find a private conversation by user ID")]
        [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> FindPrivateConversation([FromQuery] int userId)
        {
            var userUid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userUid))
                return Unauthorized("User ID not found in token");

            var currentUser = await _userRepository.GetUserByFirebaseUidAsync(userUid);
            if (currentUser == null)
                return NotFound("Current user not found");

            var conversations = await _conversationRepository.GetConversationsByUserIdAsync(currentUser.Id);

            var privateConversation = conversations.FirstOrDefault(c => c.Type == "Private" &&
                c.Participants.Any(p => p.UserId == userId));

            if (privateConversation == null)
                return NotFound("Private conversation not found");

            var response = new ConversationResponse
            {
                ConversationId = privateConversation.ConversationId,
                Type = privateConversation.Type,
                Name = privateConversation.Name,
                LastMessage = "inconstruction...",
                LastMessageAt = privateConversation.LastMessageAt,
                Participants = privateConversation.Participants.Select(p => new ParticipantDto
                {
                    UserId = p.UserId,
                    DisplayName = p.User.DisplayName
                }).ToList()
            };

            return Ok(response);
        }
    }
}
