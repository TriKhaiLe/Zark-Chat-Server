using ChatService.Application.DTOs;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController(
        IMessageRepository messageRepository,
        IChatMessageRepository chatMessageRepository,
        IConversationRepository conversationRepository,
        IUserRepository userRepository
        ) : ControllerBase
    {
        private readonly IMessageRepository _messageRepository = messageRepository;
        private readonly IConversationRepository _conversationRepository = conversationRepository;
        private readonly IChatMessageRepository _chatMessageRepository = chatMessageRepository;
        private readonly IUserRepository _userRepository = userRepository;

        [HttpGet("{userId1}/{userId2}")]
        public async Task<IActionResult> GetChatHistory(int userId1, int userId2, int pageNumber = 1, int pageSize = 20)
        {
            var messages = await _messageRepository.GetMessagesBetweenUsersAsync(userId1, userId2, pageNumber, pageSize);
            var messageDtos = messages.Select(m => new MessageDto
            {
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead
            });
            return Ok(messageDtos);
        }

        [HttpGet("conversation/{conversationId}")]
        public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userUid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userUid))
                return Unauthorized("User ID not found in token");

            var user = await _userRepository.GetUserByFirebaseUidAsync(userUid);
            if (user == null)
                return NotFound("User not found");

            var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null || !conversation.Participants.Any(p => p.UserId == user.Id))
                return Forbid("You are not a participant in this conversation");

            var messages = await _chatMessageRepository.GetMessagesByConversationIdAsync(conversationId, page, pageSize);
            var response = messages.Select(m => new
            {
                m.ChatMessageId,
                m.ConversationId,
                m.UserSendId,
                SenderUsername = m.Sender?.Username, // Nếu có navigation property
                m.Message,
                m.MediaLink,
                m.Type,
                m.SendDate
            }).ToList();

            return Ok(response);
        }
    }
}
