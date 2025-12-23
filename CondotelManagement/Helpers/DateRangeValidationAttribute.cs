using System.ComponentModel.DataAnnotations;

namespace CondotelManagement.Helpers
{
    /// <summary>
    /// Custom validation attribute để validate StartDate phải nhỏ hơn EndDate
    /// Sử dụng ở class level với các tham số: StartDatePropertyName và EndDatePropertyName
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DateRangeValidationAttribute : ValidationAttribute
    {
        public string StartDatePropertyName { get; set; } = "StartDate";
        public string EndDatePropertyName { get; set; } = "EndDate";
        public string ErrorMessageFormat { get; set; } = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.";

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var instance = validationContext.ObjectInstance;
            var startDateProperty = instance.GetType().GetProperty(StartDatePropertyName);
            var endDateProperty = instance.GetType().GetProperty(EndDatePropertyName);

            if (startDateProperty == null || endDateProperty == null)
                return ValidationResult.Success; // Nếu không tìm thấy property, bỏ qua validation

            var startDateValue = startDateProperty.GetValue(instance);
            var endDateValue = endDateProperty.GetValue(instance);

            // Nếu một trong hai giá trị null, bỏ qua (để Required attribute xử lý)
            if (startDateValue == null || endDateValue == null)
                return ValidationResult.Success;

            // Convert về DateOnly
            DateOnly? startDate = null;
            DateOnly? endDate = null;

            if (startDateValue is DateOnly sd)
                startDate = sd;
            else if (startDateValue is DateTime dt1)
                startDate = DateOnly.FromDateTime(dt1);

            if (endDateValue is DateOnly ed)
                endDate = ed;
            else if (endDateValue is DateTime dt2)
                endDate = DateOnly.FromDateTime(dt2);

            if (!startDate.HasValue || !endDate.HasValue)
                return ValidationResult.Success;

            // Validate StartDate < EndDate
            if (startDate.Value >= endDate.Value)
            {
                var errorMessage = string.IsNullOrEmpty(ErrorMessage)
                    ? ErrorMessageFormat
                    : ErrorMessage;

                return new ValidationResult(
                    errorMessage,
                    new[] { StartDatePropertyName, EndDatePropertyName }
                );
            }

            return ValidationResult.Success;
        }
    }
}

