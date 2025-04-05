using ChatService.Controllers.RequestModels;
using ChatService.Core.Entities;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Repositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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

    }
}
