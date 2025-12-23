using CondotelManagement.Models;

namespace CondotelManagement.Repositories
{
    public interface ICondotelRepository
    {
        IEnumerable<Condotel> GetCondtels();
        Condotel GetCondotelById(int id);
        void AddCondotel(Condotel condotel);
        void UpdateCondotel(Condotel condotel);
        void DeleteCondotel(int id);
        bool SaveChanges();
        Promotion? GetPromotionById(int promotionId);

        IEnumerable<Condotel> GetCondtelsByHost(int hostId);
		IEnumerable<Condotel> GetCondotelsByFilters(
			string? name,
			string? location,
			int? locationId,
			DateOnly? fromDate,
			DateOnly? toDate,
			decimal? minPrice,
			decimal? maxPrice,
			int? beds,
			int? bathrooms);
		(int TotalCount, IEnumerable<Condotel> Items) GetCondtelsByHostPaged(int hostId, int pageNumber, int pageSize);
		(int TotalCount, IEnumerable<Condotel> Items) GetCondtelsByHostPagedWithStatus(int hostId, string status, int pageNumber, int pageSize);
		(int TotalCount, IEnumerable<Condotel> Items) GetCondotelsByFiltersPaged(
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

		// Validation methods
		bool ResortExists(int? resortId);
		bool AmenitiesExist(List<int>? amenityIds);
		bool UtilitiesExist(List<int>? utilityIds);
		bool HostExists(int hostId);
		bool HasActiveBookings(int condotelId);
		
		// Methods for adding child entities
		void AddCondotelImages(IEnumerable<CondotelImage> images);
		void AddCondotelPrices(IEnumerable<CondotelPrice> prices);
		void AddCondotelDetails(IEnumerable<CondotelDetail> details);
		void AddCondotelAmenities(IEnumerable<CondotelAmenity> amenities);
		void AddCondotelUtilities(IEnumerable<CondotelUtility> utilities);
		
		// Method to update status only
		void UpdateCondotelStatus(int condotelId, string status);
	}
}
