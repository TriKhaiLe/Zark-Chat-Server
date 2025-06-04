using ChatService.Controllers.RequestModels;
using ChatService.Controllers.ResponseModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Helper;
using ChatService.Infrastructure.Repositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Security.Claims;
using EnvironmentName = Microsoft.AspNetCore.Hosting.EnvironmentName;

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
                // Validate user account
                var email = request.Email;
                var password = request.Password;
                var displayName = request.DisplayName;

                //if (displayName == null)
                //{
                //    return BadRequest(new { message = "Name must not be empty." });
                //}

                //if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                //{
                //    return BadRequest(new { message = "Email and password must not be empty." });
                //}

                //var errors = AuthValidation.ValidatePassword(request.Password);

                //if (errors.Any())
                //{
                //    return BadRequest(new
                //    {
                //        message = "Invalid password.",
                //        errors
                //    });
                //}

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
                    Email = request.Email,
                    DisplayName = string.IsNullOrEmpty(request.DisplayName)
                        ? request.Email.Split('@')[0]
                        : request.DisplayName
                };

                await _userRepository.AddUserAsync(newUser);

                if (!string.IsNullOrEmpty(request.FcmToken))
                {
                    await _userRepository.AddFcmTokenAsync(newUser.Id, request.FcmToken);
                }

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

        [HttpPost("syncData")]
        public async Task<IActionResult> SyncEmail()
        {
            try
            {
                var userList = await _authenticationService.SyncEmailAsync();

                foreach (var user in userList)
                {
                    var newUser = new User
                    {
                        FirebaseUid = user.Uid,
                        Email = user.Email,
                        DisplayName = user.Email.Split('@')[0]
                    };
                    await _userRepository.AddUserAsync(newUser);
                }

                return Ok(new { message = "Sync data user successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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

            var user = await _userRepository.GetUserByFirebaseUidAsync(localId);

            if (user == null)
            {
                return Unauthorized("User not found in system");
            }

            // Xử lý FCM Token nếu client gửi lên
            if (!string.IsNullOrEmpty(loginRequest.FcmToken))
            {
                await _userRepository.RemoveAllFcmTokensAsync(user.Id);
                await _userRepository.AddFcmTokenAsync(user.Id, loginRequest.FcmToken);
            }

            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id
            });
        }

        [HttpPost("update-user")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            var userUid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userUid))
                return Unauthorized("User ID not found in token");
            var user = await _userRepository.GetUserByFirebaseUidAsync(userUid);
            if (user == null)
                return NotFound("User not found");
            await _userRepository.UpdateUserAsync(user.Id, request.DisplayName, request.AvatarUrl, request.PublicKey);
            return Ok(new { message = "User updated successfully." });
        }

        [HttpPost("update-public-key")]
        [Authorize]
        public async Task<IActionResult> UpdatePublicKey([FromBody] string publicKey)
        {
            var userUid = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userUid))
                return Unauthorized("User ID not found in token");
            var user = await _userRepository.GetUserByFirebaseUidAsync(userUid);
            if (user == null)
                return NotFound("User not found");
            await _userRepository.UpdateUserAsync(user.Id, null, null, publicKey);
            return Ok(new { message = "Public key updated successfully." });
        }

        [HttpPost("get-public-keys")]
        [Authorize]
        public async Task<IActionResult> GetPublicKeys([FromBody] List<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
                return BadRequest("userIds is required");
            var publicKeys = await _userRepository.GetPublicKeysByUserIdsAsync(userIds);
            return Ok(publicKeys);
        }

        // [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? name,
            [FromQuery] string? email,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { statusCode = 400, message = "Please provide at least a name or an email to search." });
            }

            try
            {
                var (users, totalItems) = await _userRepository.GetUserByNameOrEmailAsync(name, email, page, pageSize);
        
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                return Ok(new
                {
                    statusCode = 200,
                    data = users,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems,
                        totalPages
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { statusCode = 404, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { statusCode = 500, message = "Internal server error", detail = ex.Message });
            }
        }


    }
}