using ChatService.Core.Entities;
using ChatService.Core.Interfaces;

namespace ChatService.Application.Services
{
    public class ChatService
    {
        private readonly IMessageRepository _messageRepository;

        public ChatService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task SendMessageAsync(string senderId, string receiverId, string content)
        {
            var conversationId = GetConversationId(senderId, receiverId);
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Type = "Text",
                Status = "Sent",
                Timestamp = DateTime.UtcNow
            };
            await _messageRepository.AddMessageAsync(conversationId, message);
        }

        public async Task<List<Message>> GetMessagesAsync(string conversationId, int limit = 50)
        {
            return await _messageRepository.GetMessagesAsync(conversationId, limit);
        }

        private string GetConversationId(string senderId, string receiverId)
        {
            var ids = new[] { senderId, receiverId }.OrderBy(id => id).ToArray();
            return $"{ids[0]}_{ids[1]}";
        }
    }
}