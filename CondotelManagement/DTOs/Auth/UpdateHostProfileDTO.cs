using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs
{
	public class UpdateHostProfileDTO
	{
		// HOST
		[Required(ErrorMessage = "Tên công ty không được để trống.")]
		[MaxLength(200, ErrorMessage = "Tên công ty không được vượt quá 200 ký tự.")]
		public string CompanyName { get; set; }
		[Required(ErrorMessage = "Địa chỉ không được để trống.")]
		[MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
		public string Address { get; set; }
		[Required(ErrorMessage = "Số điện thoại liên hệ không được để trống.")]
		[Phone(ErrorMessage = "Số điện thoại liên hệ không hợp lệ.")]
		[RegularExpression("^(0[0-9]{9,10})$", ErrorMessage = "Số điện thoại liên hệ không hợp lệ.")]
		public string PhoneContact { get; set; }

		// USER
		[Required(ErrorMessage = "Họ và tên không được để trống.")]
		[MaxLength(150, ErrorMessage = "Họ và tên không được vượt quá 150 ký tự.")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "Số điện thoại không được để trống.")]
		[RegularExpression("^(0[0-9]{9,10})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
		public string Phone { get; set; }

		[Required(ErrorMessage = "Giới tính không được để trống.")]
		[RegularExpression("^(Nam|Nữ|Khác)$", ErrorMessage = "Giới tính phải là Nam, Nữ hoặc Khác.")]
		public string Gender { get; set; }

		[DataType(DataType.Date)]
		public DateOnly? DateOfBirth { get; set; }

		[Required(ErrorMessage = "Địa chỉ người dùng không được để trống.")]
		[MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
		public string UserAddress { get; set; }

		[Url(ErrorMessage = "URL ảnh không hợp lệ.")]
		public string ImageUrl { get; set; }

		// Wallet
		[Required(ErrorMessage = "Tên ngân hàng không được để trống.")]
		[MaxLength(100, ErrorMessage = "Tên ngân hàng không được vượt quá 100 ký tự.")]
		public string BankName { get; set; }

		[Required(ErrorMessage = "Số tài khoản không được để trống.")]
		[RegularExpression("^[0-9]{6,20}$", ErrorMessage = "Số tài khoản phải là số và dài 6–20 ký tự.")]
		public string AccountNumber { get; set; }

		[Required(ErrorMessage = "Tên chủ tài khoản không được để trống.")]
		[MaxLength(150, ErrorMessage = "Tên chủ tài khoản không được vượt quá 150 ký tự.")]
		public string AccountHolderName { get; set; }
	}
}
