using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Application.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageRepository _messageRepository;

        public ChatHub(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task SendPrivateMessage(int receiverId, string content)
        {
            var senderId = GetUserIdFromContext(); 
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _messageRepository.AddMessageAsync(message);

            var receiver = await _messageRepository.GetUserByIdAsync(receiverId);
            if (receiver?.ConnectionId != null)
            {
                await Clients.Client(receiver.ConnectionId)
                    .SendAsync("ReceiveMessage", senderId, content);
            }

            await Clients.Caller.SendAsync("ReceiveMessage", senderId, content);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext(); // Implement your auth logic
            await _messageRepository.UpdateUserConnectionIdAsync(userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        private int GetUserIdFromContext()
        {
            return 1; // Placeholder
        }
    }
}
