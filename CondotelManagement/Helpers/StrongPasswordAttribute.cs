using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CondotelManagement.Validation
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string password || string.IsNullOrEmpty(password))
                return false;

            if (password.Length < 8)
                return false;

            bool hasUpper = Regex.IsMatch(password, @"[A-Z]");     // ít nhất 1 chữ hoa
            bool hasLower = Regex.IsMatch(password, @"[a-z]");     // ít nhất 1 chữ thường
            bool hasDigit = Regex.IsMatch(password, @"[0-9]");     // ít nhất 1 số

            return hasUpper && hasLower && hasDigit;
        }

        public override string FormatErrorMessage(string name)
        {
            return "Mật khẩu phải từ 8 ký tự trở lên và chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số.";
        }
    }
}