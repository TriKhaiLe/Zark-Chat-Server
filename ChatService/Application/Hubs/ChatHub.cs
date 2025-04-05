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

        public async Task SendPrivateMessage(int senderId, int receiverId, string content)
        {
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _messageRepository.AddMessageAsync(message);

            var receiver = await _userRepository.GetUserByIdAsync(receiverId);
            if (receiver?.Connections != null 
                && 
                receiver.Connections.Count != 0)
            {
                var connectionIds = receiver.Connections.Select(c => c.ConnectionId).ToList();
                await Clients.Clients(connectionIds)
                    .SendAsync("ReceiveMessage", senderId, content);
            }

            await Clients.Caller.SendAsync("ReceiveMessage", senderId, content);
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
                var user = await _userRepository.GetUserByFirebaseUid(uid); 
                if (user != null)
                {
                    await _userRepository.AddConnectionIdAsync(user.Id, Context.ConnectionId);
                    Console.WriteLine($"ConnectionId {Context.ConnectionId} được gán cho user {uid}");
                }
                else
                {
                    Console.WriteLine($"Không tìm thấy user với UID: {uid}");
                }

                await base.OnConnectedAsync();
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Xác minh token thất bại: {ex.Message}");
                throw new HubException("Token không hợp lệ");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var senderUid = Context.User?.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(senderUid))
            {
                var user = await _userRepository.GetUserByFirebaseUid(senderUid);
                if (user != null)
                {
                    await _userRepository.RemoveConnectionIdAsync(user.Id, Context.ConnectionId);
                    Console.WriteLine($"Đã xóa ConnectionId {Context.ConnectionId} khỏi user {senderUid}");
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
