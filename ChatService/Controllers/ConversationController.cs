using ChatService.Application.DTOs;
using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<int> CreateConversation(CreateConversationRequest request)
        {
            // Validation tự động nếu dùng Data Annotations hoặc Fluent Validation
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
            return conversation.ConversationId;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userUid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userUid))
                return Unauthorized("User ID not found in token");

            var user = await _userRepository.GetUserByFirebaseUidAsync(userUid);
            if (user == null)
                return NotFound("User not found");

            var conversations = await _conversationRepository.GetConversationsByUserIdAsync(user.Id);
            var response = conversations.Select(c => new
            {
                c.ConversationId,
                c.Type,
                c.Name,
                c.LastMessageAt,
                Participants = c.Participants.Select(p => new { p.UserId, p.User.Username })
            }).ToList();

            return Ok(response);
        }
    }
}
