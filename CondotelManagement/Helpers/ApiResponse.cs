using System.Text.Json.Serialization;

namespace CondotelManagement.Helpers
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Message { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public T? Data { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Dictionary<string, string[]>? Errors { get; set; }

		public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T> { Success = true, Data = data, Message = message };
        }

		public static ApiResponse<T> SuccessResponse(string? message = null)
		{
			return new ApiResponse<T> { Success = true, Message = message };
		}

		public static ApiResponse<T> Fail(string message)
        {
            return new ApiResponse<T> { Success = false, Message = message };
        }

		public static ApiResponse<T> Fail(string message, Dictionary<string, string[]>? errors = null)
		{
			return new ApiResponse<T>
			{
				Success = false,
				Message = message,
				Errors = errors
			};
		}

		public static ApiResponse<T> Fail(Dictionary<string, string[]> errors)
		{
			return new ApiResponse<T>
			{
				Success = false,
				Errors = errors
			};
		}
	}
}
