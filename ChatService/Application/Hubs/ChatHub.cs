﻿using ChatService.Core.Entities;
using Microsoft.AspNetCore.SignalR;
using FirebaseAdmin.Auth;
using ChatService.Core.Interfaces;
using FirebaseAdmin.Messaging;

namespace ChatService.Application.Hubs
{
    public class ChatHub(
        IChatMessageRepository messageRepository,
        IUserRepository userRepository,
        IConversationRepository conversationRepository) : Hub
    {
        private readonly IChatMessageRepository _messageRepository = messageRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IConversationRepository _conversationRepository = conversationRepository;

        public async Task SendMessage(int conversationId, int senderId, string content, string messageType = "Text")
        {
            var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
            {
                throw new HubException("Conversation does not exist");
            }

            var isParticipant = conversation.Participants.Any(p => p.UserId == senderId);
            if (!isParticipant)
            {
                throw new HubException("User does not belong to this conversation");
            }

            var message = new ChatMessage
            {
                ConversationId = conversationId,
                UserSendId = senderId,
                Message = content,
                Type = messageType,
                SendDate = DateTime.UtcNow,
                Status = "Sent"
            };

            await _messageRepository.AddMessageAsync(message);
            await _conversationRepository.UpdateLastMessageTimeAsync(conversationId, DateTime.UtcNow);

            // Get the list of ConnectionIds of all members in the conversation
            var participantUserIds = conversation.Participants.Select(p => p.UserId).ToList();
            var connections = await _userRepository.GetConnectionsByUserIdsAsync(participantUserIds);
            var connectionIds = connections.Select(c => c.ConnectionId).ToList();
            var onlineUserIds = connections.Select(c => c.UserId).Distinct().ToList();

            // Send the message to all members in the conversation
            if (connectionIds.Any())
            {
                await Clients.Clients(connectionIds)
                    .SendAsync("ReceiveMessage", conversationId, senderId, content, messageType, message.SendDate, message.Status, message.ChatMessageId);

                // Mark as Received for online recipients
                var recipientConnectionIds = connections
                    .Where(c => c.UserId != senderId)
                    .Select(c => c.ConnectionId)
                    .ToList();
                if (recipientConnectionIds.Any())
                {
                    await _messageRepository.UpdateMessageStatusAsync(message.ChatMessageId, "Received");
                    await Clients.Clients(recipientConnectionIds)
                        .SendAsync("UpdateMessageStatus", message.ChatMessageId, "Received");
                }
            }

            // Send push notifications to offline users
            var offlineUserIds = participantUserIds
                .Where(id => id != senderId && !onlineUserIds.Contains(id))
                .ToList();
            if (offlineUserIds.Any())
            {
                var fcmTokens = await _userRepository.GetFcmTokensByUserIdsAsync(offlineUserIds);
                if (fcmTokens.Any())
                {
                    // Lấy displayName của sender
                    var sender = await _userRepository.GetUserByIdAsync(senderId);
                    var senderName = sender?.DisplayName ?? $"User {senderId}";
                    var fcmMessage = new MulticastMessage
                    {
                        Tokens = fcmTokens,
                        Notification = new Notification
                        {
                            Title = senderName,
                            Body = content
                        },
                        Data = new Dictionary<string, string>
                        {
                            { "conversationId", conversationId.ToString() },
                            { "messageId", message.ChatMessageId.ToString() },
                            { "senderId", senderId.ToString() }
                        }
                    };
                    await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(fcmMessage);
                }
            }
        }

        public async Task Typing(int conversationId, int senderId)
        {
            var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
            {
                throw new HubException("Conversation does not exist");
            }

            var isParticipant = conversation.Participants.Any(p => p.UserId == senderId);
            if (!isParticipant)
            {
                throw new HubException("User does not belong to this conversation");
            }

            var participantUserIds = conversation.Participants.Select(p => p.UserId).Where(id => id != senderId).ToList();
            var connections = await _userRepository.GetConnectionsByUserIdsAsync(participantUserIds);
            var connectionIds = connections.Select(c => c.ConnectionId).ToList();

            // get name of sender
            var sender = await _userRepository.GetUserByIdAsync(senderId);
            if (sender == null)
            {
                throw new HubException("Sender does not exist");
            }

            if (connectionIds.Any())
            {
                await Clients.Clients(connectionIds)
                    .SendAsync("UserTyping", conversationId, sender.DisplayName, sender.AvatarUrl);
            }
        }

        public override async Task OnConnectedAsync()
        {
            var token = Context.GetHttpContext()?.Request.Query["access_token"];
            if (string.IsNullOrEmpty(token))
            {
                throw new HubException("Token was not provided");
            }

            try
            {
                // Verify token using Firebase Admin SDK
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var uid = decodedToken.Uid;
                Console.WriteLine($"User connected with UID: {uid}");

                // Save connection information
                var user = await _userRepository.GetUserByFirebaseUidAsync(uid);
                if (user != null)
                {
                    await _userRepository.AddConnectionIdAsync(user.Id, Context.ConnectionId);
                    Console.WriteLine($"ConnectionId {Context.ConnectionId} assigned to user {uid}");
                }
                else
                {
                    Console.WriteLine($"User not found with UID: {uid}");
                }

                await base.OnConnectedAsync();
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Token verification failed: {ex.Message}");
                throw new HubException("Invalid token");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var senderUid = Context.User?.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(senderUid))
            {
                var user = await _userRepository.GetUserByFirebaseUidAsync(senderUid);
                if (user != null)
                {
                    await _userRepository.RemoveConnectionIdAsync(user.Id, Context.ConnectionId);
                    Console.WriteLine($"Removed ConnectionId {Context.ConnectionId} from user {senderUid}");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}