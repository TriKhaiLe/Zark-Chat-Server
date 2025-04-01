using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IShardManager _shardManager;

        public MessageRepository(IShardManager shardManager)
        {
            _shardManager = shardManager;
        }

        public async Task AddMessageAsync(string conversationId, Message message)
        {
            using var context = await _shardManager.GetShardContextAsync(conversationId);
            context.Messages.Add(message);
            await context.SaveChangesAsync();
        }

        public async Task<List<Message>> GetMessagesAsync(string conversationId, int limit = 50)
        {
            using var context = await _shardManager.GetShardContextAsync(conversationId);
            return await context.Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }
}
