using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.DTOs
{
    public class AddUtilityToResortDTO
    {
        [Required(ErrorMessage = "UtilityId không được để trống.")]
        public int UtilityId { get; set; }

        [MaxLength(50, ErrorMessage = "Status không được vượt quá 50 ký tự.")]
        public string Status { get; set; } = "Active";

        [MaxLength(100, ErrorMessage = "OperatingHours không được vượt quá 100 ký tự.")]
        public string? OperatingHours { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Cost phải từ 0 đến 9,999,999,999.99.")]
        public decimal? Cost { get; set; }

        [MaxLength(500, ErrorMessage = "DescriptionDetail không được vượt quá 500 ký tự.")]
        public string? DescriptionDetail { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "MaximumCapacity phải lớn hơn 0.")]
        public int? MaximumCapacity { get; set; }
    }
}



