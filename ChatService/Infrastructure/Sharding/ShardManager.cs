using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Infrastructure.Sharding
{
    public class ShardManager : IShardManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IShardDistributor _shardDistributor;
        private readonly ChatDbContext _metaContext; // Context cho bảng metadata

        public ShardManager(IServiceProvider serviceProvider, IShardDistributor shardDistributor, ChatDbContext metaContext)
        {
            _serviceProvider = serviceProvider;
            _shardDistributor = shardDistributor;
            _metaContext = metaContext;
        }

        public async Task<ChatDbContext> GetShardContextAsync(string conversationId)
        {
            var shard = await _metaContext.ConversationShards
                .FirstOrDefaultAsync(s => s.ConversationId == conversationId);

            if (shard == null)
            {
                var connectionString = await _shardDistributor.GetShardConnectionStringAsync(conversationId);
                shard = new Core.Entities.ConversationShard
                {
                    ConversationId = conversationId,
                    ShardId = _shardDistributor.GetShardIndex(connectionString),
                    ShardConnectionString = connectionString
                };
                _metaContext.ConversationShards.Add(shard);
                await _metaContext.SaveChangesAsync();
            }

            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseNpgsql(shard.ShardConnectionString)
                .Options;
            var context = new ChatDbContext(options, $"Messages_{conversationId}");
            await context.Database.EnsureCreatedAsync(); // Tạo bảng nếu chưa có
            return context;
        }
    }
}
