using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly ChatDbContext _context;

        public ConversationRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        public async Task UpdateLastMessageTimeAsync(int conversationId, DateTime lastMessageAt)
        {
            var conversation = await GetConversationByIdAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessageAt = lastMessageAt;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddConversationAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Conversation>> GetConversationsByUserIdAsync(int userId)
        {
            return await _context.Conversations
                .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                .Where(c => c.Participants.Any(p => p.UserId == userId))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public Task SaveGroupSessionKeyAsync(int conversationId, Dictionary<int, string> encryptedSessionKeys)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<int, string>> GetGroupSessionKeysAsync(int conversationId)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetLastMessage(int conversationId)
        {
            var lastMessage = await _context.ChatMessages
                .Where(c => c.ConversationId == conversationId)
                .OrderByDescending(c => c.SendDate)
                .FirstOrDefaultAsync();
            if (lastMessage?.Message != null) return lastMessage.Message;
            throw new Exception("Message not found");
        }
    }
}