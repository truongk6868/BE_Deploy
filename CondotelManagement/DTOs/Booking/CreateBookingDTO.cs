using System.ComponentModel.DataAnnotations;
using CondotelManagement.Helpers;

namespace CondotelManagement.DTOs.Booking
{
    [DateRangeValidation(
        StartDatePropertyName = "StartDate",
        EndDatePropertyName = "EndDate",
        ErrorMessage = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.")]
    public class CreateBookingDTO
    {
        [Required(ErrorMessage = "CondotelId không được để trống.")]
        public int CondotelId { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
        public DateOnly EndDate { get; set; }
        public string? GuestFullName { get; set; }
        public string? GuestIdNumber { get; set; }
        public string? GuestPhone { get; set; }
        public int? PromotionId { get; set; }

        public string? VoucherCode { get; set; }

        public List<ServicePackageSelectionDTO>? ServicePackages { get; set; }
    }

   
}
