using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace ChatService.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ChatDbContext _context;

        public MessageRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<Message> AddMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<Message>> GetMessagesBetweenUsersAsync(int user1Id, int user2Id)
        {
            var messages = await _context.Messages
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                           (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return messages;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task UpdateUserConnectionIdAsync(int userId, string connectionId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ConnectionId = connectionId;
                await _context.SaveChangesAsync();
            }
        }
    }
}
