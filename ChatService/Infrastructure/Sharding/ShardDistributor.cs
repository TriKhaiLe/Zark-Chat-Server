using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Sharding
{
    public class ShardDistributor : IShardDistributor
    {
        private readonly List<string> _shardConnections; // Danh sách connection strings
        private readonly ChatDbContext _metaContext;

        public ShardDistributor(IConfiguration configuration, ChatDbContext metaContext)
        {
            _shardConnections = configuration.GetSection("ShardConnections").Get<List<string>>();
            _metaContext = metaContext;
        }

        public async Task<string> GetShardConnectionStringAsync(string conversationId)
        {
            var shardCounts = await _metaContext.ConversationShards
                .GroupBy(s => s.ShardId)
                .Select(g => new { ShardId = g.Key, Count = g.Count() })
                .ToListAsync();

            var minShard = shardCounts.OrderBy(s => s.Count).FirstOrDefault()?.ShardId ?? 0;
            return _shardConnections[minShard];
        }

        public int GetShardIndex(string connectionString)
        {
            return _shardConnections.IndexOf(connectionString);
        }
    }
}