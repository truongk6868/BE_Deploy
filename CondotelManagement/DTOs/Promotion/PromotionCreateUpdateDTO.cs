using System.ComponentModel.DataAnnotations;
using CondotelManagement.Helpers;

namespace CondotelManagement.DTOs
{
    [DateRangeValidation(
        StartDatePropertyName = "StartDate",
        EndDatePropertyName = "EndDate",
        ErrorMessage = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.")]
    public class PromotionCreateUpdateDTO
    {
        [Required(ErrorMessage = "Tên khuyến mãi không được để trống.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
        public DateOnly EndDate { get; set; }

        [Required(ErrorMessage = "Phần trăm giảm giá không được để trống.")]
        [Range(0.01, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0.01 đến 100.")]
        public decimal DiscountPercentage { get; set; }

        public string? TargetAudience { get; set; }

        public string Status { get; set; } = "Active";

        public int? CondotelId { get; set; }
    }
}








