using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories
{
    public class UserRepository(ChatDbContext context) : IUserRepository
    {
        private readonly ChatDbContext _context = context;

        public async Task<User> GetUserByFirebaseUidAsync(string firebaseUid)
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

        public async Task<List<User>> GetContactsAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Connections)
                .SingleOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new List<User>();
            }

            var contacts = await _context.Users
                .Where(u => u.Id != userId)
                .ToListAsync();

            return contacts;
        }
        public async Task<List<UserConnection>> GetConnectionsByUserIdsAsync(List<int> userIds)
        {
            return await _context.UserConnections
                .Where(uc => userIds.Contains(uc.UserId))
                .ToListAsync();
        }

        public async Task AddUserAsync(User newUser)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUid == newUser.FirebaseUid);
            if (existingUser != null)
            {
                throw new Exception("User already exists in the system.");
            }

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetUsersByNameAsync(string query)
        {
            // Search by name (case-insensitive, approximate match)
            var users = await _context.Users
                .Where(u => u.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
            return users;
        }

        public async Task<List<User>> GetUsersByIdsAsync(List<int> allUserIds)
        {
            var users = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .ToListAsync();
            return users;
        }

        public async Task UpdateValidationAccount(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null )
            {
                user.IsValidAccount = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateEmailUserAsync(string email, string firebaseUid)
        {
            var user = await GetUserByFirebaseUidAsync(firebaseUid);

            if (user != null)
            {
                user.Email = email;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddFcmTokenAsync(int userId, string fcmToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new ArgumentException("User not found", nameof(userId));
            }

            var existingDevice = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.FcmToken == fcmToken);

            if (existingDevice != null)
            {
                // Update timestamp to keep token fresh
                existingDevice.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // Add new device token
                var newDevice = new UserDevice
                {
                    UserId = userId,
                    FcmToken = fcmToken,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserDevices.Add(newDevice);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<string?>> GetFcmTokensByUserIdsAsync(List<int> userIds)
        {
            var tokens = await _context.UserDevices
                .Where(d => userIds.Contains(d.UserId))
                .Select(d => d.FcmToken)
                .ToListAsync();
            return tokens;
        }

        public async Task RemoveAllFcmTokensAsync(int userId)
        {
            var devices = await _context.UserDevices.Where(d => d.UserId == userId).ToListAsync();
            if (devices.Any())
            {
                _context.UserDevices.RemoveRange(devices);
                await _context.SaveChangesAsync();
            }
        }
    }
}
