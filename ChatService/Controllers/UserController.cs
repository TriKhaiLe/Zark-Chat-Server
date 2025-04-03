using ChatService.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IAuthenticationService authenticationService, IJwtProvider jwtProvider)
        : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService = authenticationService;
        IJwtProvider _jwtProvider = jwtProvider;

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync(string email, string password)
        {
            var firebaseUid = await _authenticationService.RegisterAsync(email, password);
            return Ok(firebaseUid);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(string email, string password)
        {
            var result = await _jwtProvider.GetForCredentialsAsync(email, password);
            if (string.IsNullOrEmpty(result))
            {
                return Unauthorized();
            }
            return Ok(result);
        }

    }
}
