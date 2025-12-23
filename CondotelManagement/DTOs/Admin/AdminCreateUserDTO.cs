using System.ComponentModel.DataAnnotations;
using CondotelManagement.Validation; // ✅ Import namespace validation

namespace CondotelManagement.DTOs.Admin
{
    public class AdminCreateUserDTO
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StrongPassword] // ✅ Thêm attribute đã tạo
        public string Password { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$",
            ErrorMessage = "Số điện thoại phải bắt đầu bằng 03, 05, 07, 08, 09 và có 10 số")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Vai trò không được để trống!")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        [StringLength(10, ErrorMessage = "Giới tính không được quá 10 ký tự")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        [DataType(DataType.Date)]
        public DateOnly DateOfBirth { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        public string? Address { get; set; }
    }
}