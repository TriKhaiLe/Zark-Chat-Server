namespace ChatService.Core.Interfaces
{
    public interface IShardDistributor
    {
        /// <summary>
        /// Lấy chuỗi kết nối (connection string) của shard cho một ConversationId.
        /// </summary>
        /// <param name="conversationId">ID của cuộc hội thoại</param>
        /// <returns>Chuỗi kết nối tới database chứa shard</returns>
        Task<string> GetShardConnectionStringAsync(string conversationId);

        /// <summary>
        /// Lấy chỉ số (index) của shard dựa trên chuỗi kết nối.
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối của shard</param>
        /// <returns>Chỉ số shard trong danh sách</returns>
        int GetShardIndex(string connectionString);
    }
}
