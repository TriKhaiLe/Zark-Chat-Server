using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace ChatService.Infrastructure.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ChatDbContext _context;
        public ChatMessageRepository(ChatDbContext context)
        {
            _context = context;
        }
        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<ChatMessage>> GetMessagesByConversationIdAsync(int conversationId, int pageNumber, int pageSize)
        {
            return await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SendDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public Task UpdateMessageStatusAsync(int chatMessageId, string status)
        {


            var message = _context.ChatMessages.Find(chatMessageId);
            if (message != null)
            {
                message.Status = status;
                _context.ChatMessages.Update(message);
                return _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Message not found");
            }
        }
    }
}
