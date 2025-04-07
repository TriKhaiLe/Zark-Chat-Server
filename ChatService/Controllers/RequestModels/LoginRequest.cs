using System.ComponentModel;

namespace ChatService.Controllers.RequestModels
{
    public class LoginRequest
    {
        [DefaultValue("zxc@gmail.com")]
        public string Email { get; set; }

        [DefaultValue("123123")]
        public string Password { get; set; }
    }
}
