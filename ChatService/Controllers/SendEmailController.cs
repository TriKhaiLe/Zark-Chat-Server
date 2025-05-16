using ChatService.Controllers.RequestModels;
using ChatService.Core.Interfaces;
using ChatService.Helper;
using ChatService.Infrastructure.Authentication;
using ChatService.Infrastructure.Repositories;
using ChatService.Services.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SendEmailController(IUserRepository userRepository, IEmailService emailService, IMemoryCache cache) : ControllerBase
    {
        private readonly IMemoryCache _cache = cache;
        private readonly IEmailService _emailService = emailService;
        private readonly IUserRepository _userRepository = userRepository;
  

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] string email)
        {
            // 1. Kiểm tra nếu email này đã gửi gần đây
            var rateLimitKey = $"OTP_RATE_{email}";
            if (_cache.TryGetValue(rateLimitKey, out _))
            {
                return BadRequest(new { message = "Bạn chỉ có thể yêu cầu OTP mỗi 30 giây." });
            }

            // 2. Tạo OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var subject = "Mã xác thực OTP";
            var body = EmailHelper.GenerateOtpBody(otp);

            await _emailService.SendEmailAsync(email, subject, body);

            // 3. Lưu OTP (5 phút) và thời gian gửi (60 giây để rate limit)
            _cache.Set($"OTP_{email}", otp, TimeSpan.FromMinutes(5));
            _cache.Set(rateLimitKey, true, TimeSpan.FromSeconds(60)); // Giới hạn 60s

            return Ok(new { message = "OTP sent to your email." });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpRequest request)
        {
            if (_cache.TryGetValue($"OTP_{request.Email}", out string cachedOtp))
            {
                if (cachedOtp == request.Otp)
                {
                    _cache.Remove($"OTP_{request.Email}"); // Xác thực xong thì xóa
                    await _userRepository.UpdateValidationAccount(request.Email);
                    return Ok(new { message = "Verify email successfully!" });
                }
                return BadRequest(new { message = "Your OTP is wrong." });
            }

            return BadRequest(new { message = "OTP has expired or not exists." });
        }
    }
}
