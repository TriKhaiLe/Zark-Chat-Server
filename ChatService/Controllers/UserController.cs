using ChatService.Controllers.RequestModels;
using ChatService.Controllers.ResponseModels;
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
    public class UserController(
        IAuthenticationService authenticationService, 
        IJwtProvider jwtProvider, 
        IUserRepository userRepository)
        : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService = authenticationService;
        IJwtProvider _jwtProvider = jwtProvider;
        private readonly IUserRepository _userRepository = userRepository;

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
        {
            try
            {
                // Register the user with Firebase
                var firebaseUid = await _authenticationService.RegisterAsync(request.Email, request.Password);

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
                    DisplayName = string.IsNullOrEmpty(request.DisplayName) ? request.Email.Split('@')[0] : request.DisplayName
                };

                await _userRepository.AddUserAsync(newUser);

                return Ok(new RegisterResponse
                {
                    Message = "User registered successfully.",
                    UserId = newUser.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
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

            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id
            });
        }

        [Authorize]
        [HttpGet("find-user-by-email")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> FindUserByEmail([FromQuery] string email)
        {
            try
            {
                string uid = await _authenticationService.GetUidByEmailAsync(email);
                var user = await _userRepository.GetUserByFirebaseUidAsync(uid);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }
                return Ok(new UserDto
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName
                });
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
