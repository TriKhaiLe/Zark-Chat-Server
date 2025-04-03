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
    }
}
