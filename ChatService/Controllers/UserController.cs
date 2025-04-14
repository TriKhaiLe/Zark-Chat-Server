using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Repositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IAuthenticationService authenticationService, IJwtProvider jwtProvider, IUserRepository userRepository)
        : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService = authenticationService;
        IJwtProvider _jwtProvider = jwtProvider;
        private readonly IUserRepository _userRepository = userRepository;

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync(string email, string password, string displayName = "")
        {
            try
            {
                // Register the user with Firebase
                var firebaseUid = await _authenticationService.RegisterAsync(email, password);

                // Check if the user already exists in the database
                var existingUser = await _userRepository.GetUserByFirebaseUidAsync(firebaseUid);
                if (existingUser != null)
                {
                    return Conflict(new { message = "User already exists in the system." });
                }

                // Create a new user in the database
                var newUser = new User
                {
                    FirebaseUid = firebaseUid,
                    Username = string.IsNullOrEmpty(displayName) ? email.Split('@')[0] : displayName
                };

                await _userRepository.AddUserAsync(newUser);

                return Ok(new { message = "User registered successfully.", userId = newUser.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(LoginRequest loginRequest)
        {
            var (token, localId) = await _jwtProvider.GetForCredentialsAsync(loginRequest.Email, loginRequest.Password); 
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(localId))
            {
                return Unauthorized();
            }

            var user = await userRepository.GetUserByFirebaseUidAsync(localId);
            if (user == null)
            {
                return Unauthorized("User not found in system");
            }

            return Ok(new
            {
                token,
                userId = user.Id,
            });
        }

        [Authorize]
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts(int userId)
        {
            var contacts = await _userRepository.GetContactsAsync(userId);
            var contactDtos = contacts.Select(c => new
            {
                c.Id,
                c.Username
            });
            return Ok(contactDtos);
        }

        [Authorize]
        [HttpGet("get-id-by-email")]
        public async Task<IActionResult> GetUidByEmail(string email)
        {
            try
            {
                string uid = await _authenticationService.GetUidByEmailAsync(email);
                var user = await _userRepository.GetUserByFirebaseUidAsync(uid);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }
                return Ok(user.Id);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { error = ex.Message });
                }
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
