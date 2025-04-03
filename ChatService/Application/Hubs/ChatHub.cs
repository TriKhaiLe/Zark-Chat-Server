using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatService.Application.Hubs
{
    public class ChatHub(IMessageRepository messageRepository, IUserRepository userRepository) : Hub
    {
        private readonly IMessageRepository _messageRepository = messageRepository;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task SendPrivateMessage(int receiverId, string content)
        {
            // Lấy UID từ Firebase token
            var senderUid = Context.User?.FindFirst("sub")?.Value; // "sub" chứa UID trong Firebase token
            if (string.IsNullOrEmpty(senderUid))
            {
                throw new HubException("User not authenticated");
            }

            // Tìm senderId từ UID trong database 
            var sender = await _userRepository.GetUserByFirebaseUid(senderUid);
            if (sender == null)
            {
                throw new HubException("Sender not found");
            }

            var message = new Message
            {
                SenderId = sender.Id,
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
                    .SendAsync("ReceiveMessage", sender.Id, content);
            }

            await Clients.Caller.SendAsync("ReceiveMessage", sender.Id, content);
        }

        public override async Task OnConnectedAsync()
        {
            var token = Context.GetHttpContext()?.Request.Query["access_token"];
            if (string.IsNullOrEmpty(token))
            {
                throw new HubException("Token không được cung cấp");
            }

            try
            {
                // Xác minh token bằng Firebase Admin SDK
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var uid = decodedToken.Uid;
                Console.WriteLine($"Người dùng đã kết nối với UID: {uid}");

                // Logic bổ sung (ví dụ: lưu thông tin kết nối vào cơ sở dữ liệu)
                await base.OnConnectedAsync();
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Xác minh token thất bại: {ex.Message}");
                throw new HubException("Token không hợp lệ");
            }
        }
    }
}
