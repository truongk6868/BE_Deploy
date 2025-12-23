using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CondotelManagement.Helpers;

namespace CondotelManagement.DTOs
{
    public class CondotelCreateDTO
    {
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

    public class ImageDTO
    {
        public int ImageId { get; set; }
		[Required(ErrorMessage = "ImageUrl không được để trống.")]
		[Url(ErrorMessage = "ImageUrl không hợp lệ.")]
		[MaxLength(255, ErrorMessage = "ImageUrl không được vượt quá 255 ký tự.")]
		public string ImageUrl { get; set; }

		[MaxLength(255, ErrorMessage = "Caption không được vượt quá 255 ký tự.")]
		public string? Caption { get; set; }
	}

    [DateRangeValidation(
        StartDatePropertyName = "StartDate",
        EndDatePropertyName = "EndDate",
        ErrorMessage = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.")]
    public class PriceDTO
    {
        public int PriceId { get; set; }
		[Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
		public DateOnly StartDate { get; set; }

		[Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
		public DateOnly EndDate { get; set; }

		[Required(ErrorMessage = "BasePrice không được để trống.")]
		[Range(typeof(decimal), "0.01", "9999999999.99",
			ErrorMessage = "BasePrice phải từ 0.01 đến 9,999,999,999.99.")]
		public decimal BasePrice { get; set; }

		[Required(ErrorMessage = "PriceType không được để trống.")]
		[RegularExpression("Thường|Cuối tuần|Ngày lễ|Cao điểm",
			ErrorMessage = "PriceType phải là Thường, Cuối tuần, Ngày lễ hoặc Cao điểm.")]
		public string PriceType { get; set; }

		[MaxLength(255, ErrorMessage = "Description không được vượt quá 255 ký tự.")]
		public string Description { get; set; }
	}

    public class DetailDTO
    {
		[MaxLength(150, ErrorMessage = "BuildingName không được vượt quá 150 ký tự.")]
		public string? BuildingName { get; set; }

		[MaxLength(50, ErrorMessage = "RoomNumber không được vượt quá 50 ký tự.")]
		public string? RoomNumber { get; set; }

		[MaxLength(500, ErrorMessage = "SafetyFeatures không được vượt quá 500 ký tự.")]
		public string? SafetyFeatures { get; set; }

		[MaxLength(500, ErrorMessage = "HygieneStandards không được vượt quá 500 ký tự.")]
		public string? HygieneStandards { get; set; }
	}
}
