using CondotelManagement.DTOs;
using CondotelManagement.Models;

namespace CondotelManagement.Services
{
    public interface IPromotionService
    {
        Task<IEnumerable<PromotionDTO>> GetAllAsync();
        Task<PromotionDTO?> GetByIdAsync(int id);
        Task<IEnumerable<PromotionDTO>> GetByCondotelIdAsync(int condotelId);
		Task<ResponseDTO<PromotionDTO>> CreateAsync(PromotionCreateUpdateDTO dto);
		Task<ResponseDTO<Promotion>> UpdateAsync(int id, PromotionCreateUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
		Task<IEnumerable<PromotionDTO>> GetAllByHostAsync(int hostId);
	}
}








