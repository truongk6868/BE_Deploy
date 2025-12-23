using CondotelManagement.DTOs;
using CondotelManagement.Models;
using CondotelManagement.Helpers;

namespace CondotelManagement.Services
{
    public interface ICondotelService
    {
		CondotelDetailDTO GetCondotelById(int id);
        CondotelUpdateDTO CreateCondotel(CondotelCreateDTO condotel);
        CondotelUpdateDTO UpdateCondotel(CondotelUpdateDTO condotel);
        bool DeleteCondotel(int id);
        IEnumerable<CondotelDTO> GetCondtelsByHost(int hostId);
        IEnumerable<CondotelDTO> GetCondotelsByFilters(
            string? name, 
            string? location,
            int? locationId,
            DateOnly? fromDate, 
            DateOnly? toDate, 
            decimal? minPrice,
	        decimal? maxPrice,
	        int? beds,
	        int? bathrooms);
		PagedResult<CondotelDTO> GetCondtelsByHostPaged(int hostId, int pageNumber, int pageSize);
		PagedResult<CondotelDTO> GetCondotelsByFiltersPaged(
			string? name,
			string? location,
			int? locationId,
			DateOnly? fromDate,
			DateOnly? toDate,
			decimal? minPrice,
			decimal? maxPrice,
			int? beds,
			int? bathrooms,
			int pageNumber,
			int pageSize);
		PagedResult<CondotelDTO> GetInactiveCondotelsByHostPaged(int hostId, int pageNumber, int pageSize);
		bool ActivateCondotel(int condotelId);
	}
}
