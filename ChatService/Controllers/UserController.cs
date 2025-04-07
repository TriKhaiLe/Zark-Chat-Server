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
        public async Task<IActionResult> RegisterAsync(string email, string password)
        {
            var firebaseUid = await _authenticationService.RegisterAsync(email, password);
            return Ok(firebaseUid);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(LoginRequest loginRequest)
        {
            var (token, localId) = await _jwtProvider.GetForCredentialsAsync(loginRequest.Email, loginRequest.Password); 
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(localId))
            {
                return Unauthorized();
            }

            var user = await userRepository.GetUserByFirebaseUid(localId);
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
                var user = await _userRepository.GetUserByFirebaseUid(uid);
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
