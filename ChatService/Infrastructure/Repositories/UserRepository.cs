using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories
{
    public class UserRepository(ChatDbContext context) : IUserRepository
    {
        private readonly ChatDbContext _context = context;

        public async Task<User> GetUserByFirebaseUid(string firebaseUid)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            // lấy user kèm connection
            var users = await _context.Users
                .Include(u => u.Connections)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return users == null ? throw new Exception($"User with ID {userId} not found.") : users;
        }

        public async Task AddConnectionIdAsync(int userId, string connectionId)
        {
            var user = await _context.Users
                .Include(u => u.Connections)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null && !user.Connections.Any(c => c.ConnectionId == connectionId))
            {
                user.Connections.Add(new UserConnection { ConnectionId = connectionId });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveConnectionIdAsync(int userId, string connectionId)
        {
            var user = await _context.Users
                .Include(u => u.Connections)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                var connection = user.Connections.FirstOrDefault(c => c.ConnectionId == connectionId);
                if (connection != null)
                {
                    user.Connections.Remove(connection);
                    await _context.SaveChangesAsync();
                }
            }
        }

    }
}
