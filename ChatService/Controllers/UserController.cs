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

            //if (!user.IsValidAccount)
            //{
            //    return StatusCode(403, new
            //    {
            //        error = "Account is invalid, please verify to continue."
            //    });
            //}

            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id
            });
        }
    }
}