namespace CondotelManagement.DTOs.Admin
{
    using System.ComponentModel.DataAnnotations;

    public class AdminUpdateUserDTO
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$",
        ErrorMessage = "Số điện thoại phải bắt đầu bằng 03, 05, 07, 08, 09 và có 10 số")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống!")]
        [Range(1, int.MaxValue, ErrorMessage = "Vai trò không được để trống!")]
        public int RoleId { get; set; } 

        public string Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Address { get; set; }
    }
}
