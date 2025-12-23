namespace CondotelManagement.DTOs.Booking
{
    public class ServiceResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object? Data { get; set; }

        public static ServiceResultDTO Ok(string message, object data = null)
            => new ServiceResultDTO { Success = true, Message = message, Data = data };

        public static ServiceResultDTO Fail(string message)
            => new ServiceResultDTO { Success = false, Message = message, Data = null };
    }

}
