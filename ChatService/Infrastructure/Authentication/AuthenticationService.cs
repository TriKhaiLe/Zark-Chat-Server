using ChatService.Controllers;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Authentication
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly FirebaseAuth _firebaseAuth;

        public AuthenticationService()
        {
            _firebaseAuth = FirebaseAuth.DefaultInstance;
        }

        public async Task<string> RegisterAsync(string email, string password)
        {
            var userArgs = new UserRecordArgs
            {
                Email = email,
                Password = password
            };
            var userRecord = await _firebaseAuth.CreateUserAsync(userArgs);
            return userRecord.Uid;
        }

        public async Task<string> GetUidByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    throw new ArgumentException("Email cannot be null or empty", nameof(email));

                UserRecord userRecord = await _firebaseAuth.GetUserByEmailAsync(email);
                return userRecord.Uid;
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
                    throw new Exception($"User with email {email} not found");

                throw new Exception($"Firebase authentication error: {ex.Message}");
            }
        }

        public async Task<List<FirebaseDto>> SyncEmailAsync()
        {
            var userList = new List<FirebaseDto>();
            var pagedEnumerable = _firebaseAuth.ListUsersAsync(null);

            await foreach (var user in pagedEnumerable)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    userList.Add(new FirebaseDto
                    {
                        Email = user.Email,
                        Uid = user.Uid,
                    });
                }   
            }
            return userList;
        }

    }
}
