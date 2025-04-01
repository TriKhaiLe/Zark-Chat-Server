using Microsoft.AspNetCore.SignalR;

namespace ChatService.Application.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            var conversationId = $"{senderId}_{receiverId}";
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", senderId, content);
        }

        public async Task JoinConversation(string senderId, string receiverId)
        {
            var conversationId = $"{senderId}_{receiverId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
    }
}
