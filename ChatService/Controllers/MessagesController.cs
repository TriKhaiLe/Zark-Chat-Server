using ChatService.Controllers.RequestModels;
using ChatService.Controllers.ResponseModels;
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
    public class MessagesController(
        IChatMessageRepository chatMessageRepository,
        IConversationRepository conversationRepository,
        IUserRepository userRepository
        ) : ControllerBase
    {
        private readonly IConversationRepository _conversationRepository = conversationRepository;
        private readonly IChatMessageRepository _chatMessageRepository = chatMessageRepository;
        private readonly IUserRepository _userRepository = userRepository;

        [HttpGet("{conversationId}")]
        [SwaggerOperation(Summary = "Get messages in a conversation")]
        [ProducesResponseType(typeof(IEnumerable<MessageResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] MessageRequest request)
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

            var messages = await _chatMessageRepository.GetMessagesByConversationIdAsync(conversationId, request.Page, request.PageSize);
            var response = messages.Select(m => new MessageResponse
            {
                ChatMessageId = m.ChatMessageId,
                ConversationId = m.ConversationId,
                UserSendId = m.UserSendId,
                SenderDisplayName = m.Sender?.DisplayName,
                Message = m.Message,
                MediaLink = m.MediaLink,
                Type = m.Type,
                SendDate = m.SendDate
            }).ToList();

            return Ok(response);
        }
    }
}
