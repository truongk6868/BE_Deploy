using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CondotelManagement.DTOs
{
    public class CondotelUpdateDTO
    {
        public int CondotelId { get; set; }
        [JsonIgnore] // Không cho client set
        public int HostId { get; set; }
        public int? ResortId { get; set; }
		[Required(ErrorMessage = "Tên condotel không được để trống.")]
		[MaxLength(150, ErrorMessage = "Tên condotel không được vượt quá 150 ký tự.")]
		public string Name { get; set; }
		[MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
		public string? Description { get; set; }

		[Range(typeof(decimal), "0.01", "9999999999.99",
			ErrorMessage = "Giá theo đêm phải từ 0.01 đến 9,999,999,999.99.")]
		public decimal PricePerNight { get; set; }

		[Range(1, 20, ErrorMessage = "Số giường phải từ 1 đến 20.")]
		public int Beds { get; set; }

		[Range(1, 20, ErrorMessage = "Số phòng tắm phải từ 1 đến 20.")]
		public int Bathrooms { get; set; }

		[Required(ErrorMessage = "Trạng thái không được để trống.")]
		[RegularExpression("Active|Inactive",
			ErrorMessage = "Trạng thái chỉ chấp nhận: Active (Hoạt động) hoặc Inactive (Không hoạt động).")]
		public string Status { get; set; }

		// Liên kết 1-n
		public List<ImageDTO>? Images { get; set; }
        public List<PriceDTO>? Prices { get; set; }
        public List<DetailDTO>? Details { get; set; }

        // Liên kết n-n
        public List<int>? AmenityIds { get; set; }
        public List<int>? UtilityIds { get; set; }
    }
}
