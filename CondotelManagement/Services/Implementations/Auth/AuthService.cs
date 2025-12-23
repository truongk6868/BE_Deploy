using BCrypt.Net;
using CondotelManagement.DTOs.Auth;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Admin; // Giả sử IUserRepository ở đây
using CondotelManagement.Repositories.Interfaces.Auth;
using CondotelManagement.Services.Interfaces.Auth;
using CondotelManagement.Services.Interfaces.Shared;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CondotelManagement.Services.Implementations.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepo;

        public AuthService(IAuthRepository repo, IConfiguration config, IHttpContextAccessor httpContextAccessor, IEmailService emailService, IUserRepository userRepo)
        {
            _repo = repo;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _userRepo = userRepo;
        }

        public async Task<object?> LoginAsync(LoginRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);
            if (user == null || user.Status != "Active")
                return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            // Gọi hàm helper (hàm này vẫn trả về LoginResponse)
            var tokenString = GenerateJwtTokenString(user); // Đổi tên hàm helper

            // TẠO DTO GIỐNG NHƯ TRONG GetMe
            var userProfile = new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = user.Role?.RoleName ?? "User",
                Status = user.Status,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                ImageUrl = user.ImageUrl, // Thêm ImageUrl
                CreatedAt = user.CreatedAt
            };

            // Trả về object lồng nhau mà FE mong đợi
            return new { Token = tokenString, User = userProfile };
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            // 1. Kiểm tra xem email đã tồn tại và Active chưa
            var existingUser = await _repo.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Status == "Active")
            {
                return false; // Email đã được đăng ký và kích hoạt
            }

            // 2. Tạo OTP
            string otp = new Random().Next(100000, 999999).ToString();
            DateTime expiry = DateTime.UtcNow.AddMinutes(10); // OTP hết hạn sau 10 phút

            User userToRegister;

            if (existingUser != null && existingUser.Status == "Pending")
            {
                // 3a. Nếu user tồn tại nhưng "Pending" (chưa kích hoạt)
                // Cập nhật lại mật khẩu và thông tin, gửi lại OTP
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                existingUser.FullName = request.FullName;
                existingUser.Phone = request.Phone;
                existingUser.Gender = request.Gender;
                existingUser.DateOfBirth = request.DateOfBirth;
                existingUser.Address = request.Address;

                await _userRepo.UpdateUserAsync(existingUser); // Cần IUserRepository
                userToRegister = existingUser;
            }
            else
            {
                // 3b. Tạo user mới hoàn toàn với Status = "Pending"
                userToRegister = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Phone = request.Phone,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    Address = request.Address,
                    RoleId = 3, // Role "User"
                    Status = "Pending", // QUAN TRỌNG
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.RegisterAsync(userToRegister);
            }

            // 4. Lưu OTP (dùng chung cột ResetToken) và gửi mail
            // Giả định SetPasswordResetTokenAsync lưu vào 2 cột PasswordResetToken và ResetTokenExpires
            await _repo.SetPasswordResetTokenAsync(userToRegister, otp, expiry);
            await _emailService.SendVerificationOtpAsync(userToRegister.Email, otp);

            return true;
        }

        // SỬA LỖI 2 & 3: Đổi ResetToken -> PasswordResetToken
        public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);

            // Kiểm tra user, status, OTP và thời gian hết hạn
            if (user == null ||
                user.Status != "Pending" || // Chỉ xác thực user "Pending"
                user.PasswordResetToken != request.Otp || // <-- SỬA Ở ĐÂY
                user.ResetTokenExpires < DateTime.UtcNow)
            {
                return false; // Sai OTP, hết hạn, hoặc email không hợp lệ
            }

            // 1. Kích hoạt tài khoản
            user.Status = "Active";
            // 2. Xóa OTP
            user.PasswordResetToken = null; // <-- SỬA Ở ĐÂY
            user.ResetTokenExpires = null;

            // 3. Cập nhật user (Cần IUserRepository)
            return await _userRepo.UpdateUserAsync(user);
        }

        public async Task<bool> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(user.PasswordResetToken) ||
                user.PasswordResetToken != request.Otp ||
                user.ResetTokenExpires < DateTime.UtcNow)
            {
                return false;
            }

            // Only verify validity; do not clear OTP here so it can be used for reset-password-with-otp.
            return true;
        }

        public async Task<object?> GoogleLoginAsync(GoogleLoginRequest request)
        {
            try
            {
                // ... (Toàn bộ logic try...catch của bạn giữ nguyên) ...
                var googleClientId = _config["Google:ClientId"];
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { googleClientId }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                var userEmail = payload.Email;
                var userName = payload.Name;
                var user = await _repo.GetByEmailAsync(userEmail);

                if (user == null)
                {
                    user = new User
                    {
                        FullName = userName,
                        Email = userEmail,
                        PasswordHash = "EXTERNAL_LOGIN",
                        RoleId = 3, // Role "User"
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        ImageUrl = payload.Picture // ⬅️ THÊM: Tự động lấy ảnh avatar Google
                    };
                    await _repo.RegisterAsync(user);
                    user = await _repo.GetByEmailAsync(userEmail); // Lấy lại user đã có Role
                }

                if (user.Status != "Active")
                {
                    user.Status = "Active";
                    if (string.IsNullOrEmpty(user.ImageUrl)) // ⬅️ THÊM: Cập nhật ảnh nếu chưa có
                    {
                        user.ImageUrl = payload.Picture;
                    }
                    await _userRepo.UpdateUserAsync(user);
                }

                // --- SỬA LOGIC TRẢ VỀ ---
                var tokenString = GenerateJwtTokenString(user); // Gọi helper

                var userProfile = new UserProfileDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    RoleName = user.Role?.RoleName ?? "User",
                    Status = user.Status,
                    Gender = user.Gender,
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address,
                    ImageUrl = user.ImageUrl, // Thêm ImageUrl
                    CreatedAt = user.CreatedAt
                };

                // Trả về object lồng nhau
                return new { Token = tokenString, User = userProfile };
            }
            catch (Exception ex)
            {
                // Log lỗi (ex.Message)
                return null; // Token Google không hợp lệ
            }
        }
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);
            if (user == null || user.Status != "Active")
            {
                return true;
            }
            string token = Guid.NewGuid().ToString();
            DateTime expiry = DateTime.UtcNow.AddHours(1);
            await _repo.SetPasswordResetTokenAsync(user, token, expiry);
            string resetLink = $"https://your-frontend-domain.com/reset-password?token={token}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
            return true;
        }


        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _repo.GetUserByResetTokenAsync(request.ResetToken);
            if (user == null || user.ResetTokenExpires < DateTime.UtcNow)
            {
                return false;
            }
            string newHashStr = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            var success = await _repo.UpdatePasswordAsync(user.Email, newHashStr);
            if (!success) return false;
            await _repo.SetPasswordResetTokenAsync(user, null, DateTime.UtcNow);
            return true;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var email = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return await _repo.GetByEmailAsync(email);
        }

        public async Task<bool> SendPasswordResetOtpAsync(ForgotPasswordRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);
            if (user == null || user.Status != "Active")
            {
                return true;
            }
            string otp = new Random().Next(100000, 999999).ToString();
            DateTime expiry = DateTime.UtcNow.AddMinutes(10);
            await _repo.SetPasswordResetTokenAsync(user, otp, expiry);
            await _emailService.SendPasswordResetOtpAsync(user.Email, otp);
            return true;
        }

        public async Task<bool> ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);

            // Tên cột ở đây (PasswordResetToken) đã đúng
            if (user == null ||
                user.PasswordResetToken != request.Otp ||
                user.ResetTokenExpires < DateTime.UtcNow)
            {
                return false;
            }

            string newHashStr = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            var success = await _repo.UpdatePasswordAsync(user.Email, newHashStr);
            if (!success) return false;
            await _repo.SetPasswordResetTokenAsync(user, null, DateTime.UtcNow);
            return true;
        }

        private string GenerateJwtTokenString(User user)
        {
            var roleName = user.Role?.RoleName ?? "User";
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public async Task<(bool IsSuccess, string Message)> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            var user = await _repo.GetByEmailAsync(email);
            if (user == null || user.Status != "Active")
                return (false, "Tài khoản không tồn tại hoặc chưa được kích hoạt");

            // Kiểm tra mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return (false, "Mật khẩu hiện tại không đúng");

            // Hash mật khẩu mới
            string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var success = await _repo.UpdatePasswordAsync(email, newHash);
            if (!success)
                return (false, "Có lỗi khi cập nhật mật khẩu");

            return (true, "Đổi mật khẩu thành công");
        }
        // HÀM CŨ NÀY BÂY GIỜ KHÔNG CÒN DÙNG ĐẾN (có thể xóa)
        // private LoginResponse GenerateJwtToken(User user) { ... }
    }
}
