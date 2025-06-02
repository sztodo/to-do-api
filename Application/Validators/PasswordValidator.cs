namespace To_Do_App_API.Application.Validators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    public class PasswordValidator
    {
        private static readonly List<string> WeakPasswords = new List<string>
    {
        "123456", "password", "admin", "qwerty", "abc123", "letmein", "000000"
    };

        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            // Minimum 8 characters, at least one uppercase, lowercase, digit and symbol
            var hasMinLength = password.Length >= 8;
            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasMinLength && hasUpper && hasLower && hasDigit && hasSymbol;
        }

        public static bool IsInWeakList(string password)
        {
            return WeakPasswords.Contains(password.ToLower());
        }

        public static async Task<bool> IsPasswordPwnedAsync(string password)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToUpper();

            var prefix = hashString.Substring(0, 5);
            var suffix = hashString.Substring(5);

            using var client = new HttpClient();
            var response = await client.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefix}");

            return response.Contains(suffix);
        }
    }

}
