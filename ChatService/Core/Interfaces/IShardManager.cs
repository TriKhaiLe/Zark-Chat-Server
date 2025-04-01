
using ChatService.Infrastructure.Data;

namespace ChatService.Core.Interfaces
{
    public interface IShardManager
    {
        /// <summary>
        /// Lấy hoặc tạo DbContext cho shard tương ứng với ConversationId.
        /// </summary>
        /// <param name="conversationId">ID của cuộc hội thoại</param>
        /// <returns>DbContext để truy cập shard</returns>
        Task<ChatDbContext> GetShardContextAsync(string conversationId);
    }
}