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
        IUserRepository userRepository,
        IAuthenticationService authenticationService
        ) : ControllerBase
    {
        private readonly IConversationRepository _conversationRepository = conversationRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IAuthenticationService _authenticationService = authenticationService;

        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create a new conversation")]
        [ProducesResponseType(typeof(CreateConversationResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CreateConversationResponse>> CreateConversation(CreateConversationRequest request)
        {
            if (request.ParticipantIds.Contains(request.CreatorId))
            {
                throw new ArgumentException("CreatorId cannot be in ParticipantIds");
            }

            // get name for conversation
            var allUserIds = request.ParticipantIds.Append(request.CreatorId).ToList();
            string conversationName = string.Empty;
            if (request.Type == "Private")
            {
                var otherUserId = request.ParticipantIds.FirstOrDefault();
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
                    var users = await _userRepository.GetUsersByIdsAsync(allUserIds);
                    conversationName = string.Join(", ", users.Select(u => u.DisplayName));
                }
            }

            // get participants for conversation
            var participants = new List<ConversationParticipant>();
            foreach (var userId in allUserIds)
            {
                var participant = new ConversationParticipant
                {
                    UserId = userId,
                    Role = (userId == request.CreatorId && request.Type == "Group") ? "Admin" : "Member",
                    JoinedAt = DateTime.UtcNow
                };
                participants.Add(participant);
            }

            var conversation = new Conversation
            {
                Type = request.Type,
                Name = conversationName,
                Participants = participants
            };

            // Nhận encrypted session keys từ client (client sinh session key, mã hóa bằng public key từng thành viên và gửi lên)
            conversation.EncryptedSessionKeys = request.EncryptedSessionKeys ?? [];

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

            var response = conversations.Select(async c => new ConversationResponse
            {
                ConversationId = c.ConversationId,
                Type = c.Type,
                Name = c.Name,
                LastMessage = await _conversationRepository.GetLastMessage(c.ConversationId),
                LastMessageAt = c.LastMessageAt
            }).ToList();

            return Ok(response);
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Search conversations by name or email",
            Description = "Searches for conversations by name (group or private) or finds/creates private conversation by email"
        )]
        [ProducesResponseType(typeof(IEnumerable<SearchResultResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchConversations([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty");

            var userUid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userUid))
                return Unauthorized("User ID not found in token");

            var currentUser = await _userRepository.GetUserByFirebaseUidAsync(userUid);
            if (currentUser == null)
                return NotFound("Current user not found");

            var results = new List<SearchResultResponse>();

            // Tìm cuộc trò chuyện theo tên
            var conversations = await _conversationRepository.GetConversationsByUserIdAsync(currentUser.Id);
            var matchingConversations = conversations
                .Where(c => c.Name != null && c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(c => new SearchResultResponse
                {
                    ConversationId = c.ConversationId,
                    Type = c.Type,
                    Name = c.Name,
                    IsNew = false
                }).ToList();

            results.AddRange(matchingConversations);

            // Nếu query là email, tìm user và kiểm tra cuộc trò chuyện cá nhân
            if (query.Contains('@'))
            {
                try
                {
                    string targetUid = await _authenticationService.GetUidByEmailAsync(query);
                    var targetUser = await _userRepository.GetUserByFirebaseUidAsync(targetUid);
                    if (targetUser != null && targetUser.Id != currentUser.Id)
                    {
                        var privateConversation = conversations.FirstOrDefault(c =>
                            c.Type == "Private" &&
                            c.Participants.Any(p => p.UserId == targetUser.Id));

                        if (privateConversation != null)
                        {
                            // Tránh trùng nếu đã tìm thấy qua tên
                            if (!results.Any(r => r.ConversationId == privateConversation.ConversationId))
                            {
                                results.Add(new SearchResultResponse
                                {
                                    ConversationId = privateConversation.ConversationId,
                                    Type = privateConversation.Type,
                                    Name = privateConversation.Name,
                                    Avatar = targetUser.AvatarUrl ?? "",
                                    IsNew = false
                                });
                            }
                        }
                        else
                        {
                            // User mới, đề xuất tạo cuộc trò chuyện
                            results.Add(new SearchResultResponse
                            {
                                UserId = targetUser.Id,
                                Type = "Private",
                                Name = targetUser.DisplayName,
                                Avatar = targetUser.AvatarUrl ?? "",
                                IsNew = true
                            });
                        }
                    }
                }
                catch
                {
                    // Email không tồn tại, bỏ qua
                }
            }

            return Ok(results);
        }
    }
}
