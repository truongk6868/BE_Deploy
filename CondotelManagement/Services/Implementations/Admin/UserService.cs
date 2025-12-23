using CondotelManagement.Data;
using CondotelManagement.DTOs.Admin;
using CondotelManagement.Models;
using CondotelManagement.Services.Interfaces.Admin;
using Microsoft.EntityFrameworkCore; 
using CondotelManagement.Repositories.Interfaces;
using CondotelManagement.Repositories.Interfaces.Admin;
using CondotelManagement.Repositories.Interfaces.Auth; // Cho IAuthRepository
using CondotelManagement.Services.Interfaces.Shared; // Cho IEmailService

namespace CondotelManagement.Services.Implementations.Admin
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly CondotelDbVer1Context _context;
        private readonly IAuthRepository _authRepo;
        private readonly IEmailService _emailService;

        public UserService(IUserRepository userRepository,
                             IRoleRepository roleRepository,
                             CondotelDbVer1Context context,
                             IAuthRepository authRepo, 
                             IEmailService emailService) 
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _context = context;
            _authRepo = authRepo; 
            _emailService = emailService; 
        }

        // 1. Lấy tất cả user
        public async Task<IEnumerable<UserViewDTO>> AdminGetAllUsersAsync()
        {
            // Dùng _context để có thể Include và Select (Project) sang DTO
            return await _context.Users
                .Include(u => u.Role) // Join với bảng Role
                .Select(u => new UserViewDTO // Map sang DTO
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Status = u.Status,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    Address = u.Address,
                    CreatedAt = u.CreatedAt,
                    RoleName = u.Role.RoleName // Lấy tên Role
                })
                .ToListAsync();
        }

        // 2. Lấy user theo ID
        public async Task<UserViewDTO> AdminGetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.UserId == userId)
                .Select(u => new UserViewDTO
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Status = u.Status,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    Address = u.Address,
                    CreatedAt = u.CreatedAt,
                    RoleName = u.Role.RoleName
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool IsSuccess, string Message, UserViewDTO CreatedUser)> AdminCreateUserAsync(AdminCreateUserDTO dto)
        {
            // 1. Kiểm tra RoleId có hợp lệ VÀ có phải là Admin không
            var role = await _roleRepository.GetByIdAsync(dto.RoleId);
            if (role == null)
            {
                return (false, "RoleId không hợp lệ", null);
            }
            // KIỂM TRA MỚI: Không cho phép Admin tạo Admin khác
            if (role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Không có quyền tạo user với vai trò Admin", null);
            }

            // 2. Kiểm tra email tồn tại (cho cả user "Active" và "Pending")
            var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null && existingUser.Status == "Active")
            {
                return (false, "Email đã tồn tại và đã được kích hoạt", null);
            }

            // 3. Tạo OTP
            string otp = new Random().Next(100000, 999999).ToString();
            DateTime expiry = DateTime.UtcNow.AddMinutes(10); // OTP hết hạn 10 phút

            // 4. Hash mật khẩu (Sử dụng BCrypt.Net)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            User userToRegister;

            // 5. Tạo hoặc Cập nhật Model User
            if (existingUser != null && existingUser.Status == "Pending")
            {
                // Nếu user "Pending" tồn tại, cập nhật lại thông tin
                existingUser.FullName = dto.FullName;
                existingUser.PasswordHash = passwordHash;
                existingUser.Phone = dto.Phone;
                existingUser.RoleId = dto.RoleId;
                existingUser.Gender = dto.Gender;
                existingUser.DateOfBirth = dto.DateOfBirth;
                existingUser.Address = dto.Address;

                await _userRepository.UpdateAsync(existingUser);
                userToRegister = existingUser;
            }
            else
            {
                // Tạo mới nếu chưa có
                userToRegister = new User
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PasswordHash = passwordHash,
                    Phone = dto.Phone,
                    RoleId = dto.RoleId,
                    Gender = dto.Gender,
                    DateOfBirth = dto.DateOfBirth,
                    Address = dto.Address,
                    Status = "Pending", // SỬA ĐỔI: Set "Pending"
                    CreatedAt = DateTime.UtcNow
                };
                await _userRepository.AddAsync(userToRegister);
            }

            // 6. Lưu OTP và Gửi Mail (Sử dụng dịch vụ từ Auth)
            await _authRepo.SetPasswordResetTokenAsync(userToRegister, otp, expiry);
            await _emailService.SendVerificationOtpAsync(userToRegister.Email, otp);

            // 7. Map sang DTO để trả về
            var userView = new UserViewDTO
            {
                UserId = userToRegister.UserId,
                FullName = userToRegister.FullName,
                Email = userToRegister.Email,
                Phone = userToRegister.Phone,
                Status = userToRegister.Status, // Sẽ là "Pending"
                Gender = userToRegister.Gender,
                DateOfBirth = userToRegister.DateOfBirth,
                Address = userToRegister.Address,
                CreatedAt = userToRegister.CreatedAt,
                RoleName = role.RoleName
            };

            
            return (true, "Tạo user thành công. Mã OTP đã được gửi đến email để kích hoạt.", userView);
        }

        // SỬA LẠI: Admin cập nhật user
        public async Task<(bool IsSuccess, string Message, UserViewDTO UpdatedUser)> AdminUpdateUserAsync(int userId, AdminUpdateUserDTO dto)
        {
            // SỬA ĐỔI: Phải dùng _context.Include để lấy Role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return (false, "Không tìm thấy user", null);
            }

            // 🚨 FIX 1: Kiểm tra user.Role có null không
            if (user.Role == null)
            {
                return (false, "User không có role hợp lệ trong hệ thống", null);
            }

            // KIỂM TRA MỚI: Không cho phép sửa thông tin của Admin
            if (user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Không thể chỉnh sửa thông tin của Admin", null);
            }

            // Kiểm tra email trùng lặp (nếu đổi email)
            if (user.Email != dto.Email)
            {
                var emailExists = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (emailExists != null)
                {
                    return (false, "Email mới đã được sử dụng", null);
                }
            }

            // 🚨 FIX 2: Kiểm tra RoleId có được cung cấp không
            if (dto.RoleId <= 0)
            {
                return (false, "RoleId không hợp lệ. Vui lòng chọn role.", null);
            }

            // Kiểm tra RoleId
            var newRole = await _roleRepository.GetByIdAsync(dto.RoleId);
            if (newRole == null)
            {
                return (false, "RoleId không hợp lệ", null);
            }

            // 🚨 FIX 3: Kiểm tra newRole không null trước khi truy cập
            if (newRole == null)
            {
                return (false, "Không tìm thấy role với ID đã cung cấp", null);
            }

            // KIỂM TRA MỚI: Không cho phép thăng cấp lên Admin
            if (newRole.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Không có quyền thăng cấp user thành Admin", null);
            }

            // Cập nhật thông tin
            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.RoleId = dto.RoleId;
            user.Gender = dto.Gender;
            user.DateOfBirth = dto.DateOfBirth;
            user.Address = dto.Address;

            await _userRepository.UpdateAsync(user);

            // Map sang DTO trả về
            var updatedView = await AdminGetUserByIdAsync(userId); // Gọi lại hàm GetById để lấy DTO
            return (true, "Cập nhật thành công", updatedView);
        }

        // 5. Admin reset mật khẩu

        public async Task<bool> AdminResetPasswordAsync(int userId, string newPassword)
        {
            // SỬA: Dùng _context để lấy Role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return false;

            // THÊM: Kiểm tra phân quyền
            if (user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Không cho phép reset mật khẩu của Admin khác
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user); // Dùng repo để Update là đúng
            return true;
        }

        
        public async Task<bool> AdminUpdateUserStatusAsync(int userId, string newStatus)
        {
            // (Bạn nên kiểm tra newStatus hợp lệ ở đây)

            // SỬA: Dùng _context để lấy Role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return false;

            // THÊM: Kiểm tra phân quyền
            if (user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Không cho phép thay đổi trạng thái của Admin khác
                return false;
            }

            user.Status = newStatus;
            await _userRepository.UpdateAsync(user); // Dùng repo để Update là đúng
            return true;
        }
    }
}
