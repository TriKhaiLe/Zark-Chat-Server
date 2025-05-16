using System.Text.RegularExpressions;

namespace ChatService.Helper
{
    public static class AuthValidation
    {
        public static List<string> ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password cannot be empty.");
                return errors;
            }

            if (password.Length < 7)
                errors.Add("Password must be at least 7 characters long.");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter.");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one digit.");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Password must contain at least one special character.");

            return errors;
        }
    }
}
