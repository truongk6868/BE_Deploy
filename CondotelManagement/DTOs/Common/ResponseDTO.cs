namespace CondotelManagement.DTOs
{
	public class ResponseDTO<T>
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public T Data { get; set; }

		public static ResponseDTO<T> SuccessResult(T data, string message = "")
		{
			return new ResponseDTO<T> { Success = true, Message = message, Data = data };
		}

		public static ResponseDTO<T> Fail(string message)
		{
			return new ResponseDTO<T> { Success = false, Message = message };
		}
	}

}
