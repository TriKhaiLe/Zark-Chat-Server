namespace ChatService.Helper
{
    public static class EmailHelper
    {
        public static string GenerateOtpBody(string otp)
        {
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset=""UTF-8"">
      <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
      <style>
        @media only screen and (max-width: 600px) {{
          .container {{
            width: 100% !important;
            padding: 20px !important;
          }}
          .otp {{
            font-size: 32px !important;
            letter-spacing: 10px !important;
          }}
        }}
      </style>
    </head>
    <body style=""margin: 0; padding: 0; font-family: 'Manrope', sans-serif;"">
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
        <tr>
          <td align=""center"">
            <table class=""container"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; padding: 40px; border-radius: 8px;"">
              <tr>
                <td align=""center"" style=""padding-bottom: 20px;"">
                  <img src=""https://d1hjkbq40fs2x4.cloudfront.net/2016-01-31/files/1045.jpg"" alt=""Logo"" style=""max-width: 120px; height: auto;"" />
                </td>
              </tr>
              <tr>
                <td align=""center"" style=""font-size: 24px; font-weight: bold; color: #333333; padding-bottom: 20px;"">
                  Mã xác thực
                </td>
              </tr>
              <tr>
                <td align=""center"" style=""font-size: 16px; color: #555555; padding-bottom: 30px;"">
                  Đây là mã xác thực đăng ký tài khoản của bạn:
                </td>
              </tr>
              <tr>
                <td align=""center"" style=""padding: 20px 0;"">
                  <div class=""otp"" style=""font-size: 48px; font-weight: bold; color: #000000; letter-spacing: 20px;"">{otp}</div>
                </td>
              </tr>
              <tr>
                <td align=""center"" style=""font-size: 14px; color: #888888; padding-top: 30px;"">
                  Nếu bạn không phải là người gửi yêu cầu này, hãy đổi mật khẩu tài khoản ngay lập tức để tránh việc bị truy cập trái phép.
                </td>
              </tr>
            </table>
          </td>
        </tr>
      </table>
    </body>
    </html>";
        }
    }
}
