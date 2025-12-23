using CondotelManagement.DTOs;
using CondotelManagement.Models;
using CondotelManagement.Repositories;

namespace CondotelManagement.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepo;

        public PromotionService(IPromotionRepository promotionRepo)
        {
            _promotionRepo = promotionRepo;
        }

        public async Task<IEnumerable<PromotionDTO>> GetAllAsync()
        {
            var promotions = await _promotionRepo.GetAllAsync();
            return promotions.Select(p => new PromotionDTO
            {
                PromotionId = p.PromotionId,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DiscountPercentage = p.DiscountPercentage,
                TargetAudience = p.TargetAudience,
                Status = p.Status,
                CondotelId = p.CondotelId,
                CondotelName = p.Condotel?.Name
            });
        }

        public async Task<PromotionDTO?> GetByIdAsync(int id)
        {
            var p = await _promotionRepo.GetByIdAsync(id);
            if (p == null) return null;

            return new PromotionDTO
            {
                PromotionId = p.PromotionId,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DiscountPercentage = p.DiscountPercentage,
                TargetAudience = p.TargetAudience,
                Status = p.Status,
                CondotelId = p.CondotelId,
                CondotelName = p.Condotel?.Name
            };
        }

        public async Task<IEnumerable<PromotionDTO>> GetByCondotelIdAsync(int condotelId)
        {
            var promotions = await _promotionRepo.GetByCondotelIdAsync(condotelId);
            return promotions.Select(p => new PromotionDTO
            {
                PromotionId = p.PromotionId,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DiscountPercentage = p.DiscountPercentage,
                TargetAudience = p.TargetAudience,
                Status = p.Status,
                CondotelId = p.CondotelId,
                CondotelName = p.Condotel?.Name
            });
        }

        public async Task<ResponseDTO<PromotionDTO>> CreateAsync(PromotionCreateUpdateDTO dto)
        {
		// Kiểm tra ngày logic
		if (dto.StartDate >= dto.EndDate)
			return ResponseDTO<PromotionDTO>.Fail("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

			if (dto.EndDate < DateOnly.FromDateTime(DateTime.Now))
				return ResponseDTO<PromotionDTO>.Fail("The end date cannot be in the pastứ.");

			// Kiểm tra trùng hoặc chồng thời gian
			bool hasOverlap = await _promotionRepo.CheckOverlapAsync(dto.CondotelId, dto.StartDate, dto.EndDate);
			if (hasOverlap)
				return ResponseDTO<PromotionDTO>.Fail("Promotion period overlaps or overlaps with another promotion.");

            //create
			var promotion = new Promotion
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DiscountPercentage = dto.DiscountPercentage,
                TargetAudience = dto.TargetAudience,
                Status = dto.Status,
                CondotelId = dto.CondotelId
            };

            await _promotionRepo.AddAsync(promotion);
            
            // Reload để lấy Condotel navigation property
            var created = await _promotionRepo.GetByIdAsync(promotion.PromotionId);
            if (created == null)
                throw new InvalidOperationException("Failed to retrieve created promotion");

            var result = new PromotionDTO
            {
                PromotionId = created.PromotionId,
                Name = created.Name,
                StartDate = created.StartDate,
                EndDate = created.EndDate,
                DiscountPercentage = created.DiscountPercentage,
                TargetAudience = created.TargetAudience,
                Status = created.Status,
                CondotelId = created.CondotelId,
                CondotelName = created.Condotel?.Name
            };

			return ResponseDTO<PromotionDTO>.SuccessResult(result, "Create promotion success.");
		}

        public async Task<ResponseDTO<Promotion>> UpdateAsync(int id, PromotionCreateUpdateDTO dto)
        {
			// Kiểm tra ngày logic
			if (dto.StartDate >= dto.EndDate)
				return ResponseDTO<Promotion>.Fail("Start date must be less than end date.");

			if (dto.EndDate < DateOnly.FromDateTime(DateTime.Now))
				return ResponseDTO<Promotion>.Fail("The end date cannot be in the pastứ.");

			// Kiểm tra trùng hoặc chồng thời gian (loại trừ promotion hiện tại đang update)
			bool hasOverlap = await _promotionRepo.CheckOverlapAsync(dto.CondotelId, dto.StartDate, dto.EndDate, id);
			if (hasOverlap)
				return ResponseDTO<Promotion>.Fail("Promotion period overlaps or overlaps with another promotion.");

			var promotion = await _promotionRepo.GetByIdAsync(id);
            if (promotion == null) return ResponseDTO<Promotion>.Fail("Not found.");

			promotion.Name = dto.Name;
            promotion.StartDate = dto.StartDate;
            promotion.EndDate = dto.EndDate;
            promotion.DiscountPercentage = dto.DiscountPercentage;
            promotion.TargetAudience = dto.TargetAudience;
            promotion.Status = dto.Status;
            promotion.CondotelId = dto.CondotelId;

            await _promotionRepo.UpdateAsync(promotion);
			return ResponseDTO<Promotion>.SuccessResult(promotion, "Update promotion success.");
		}

        public async Task<bool> DeleteAsync(int id)
        {
            var promotion = await _promotionRepo.GetByIdAsync(id);
            if (promotion == null) return false;

            await _promotionRepo.DeleteAsync(promotion);
            return true;
        }

		public async Task<IEnumerable<PromotionDTO>> GetAllByHostAsync(int hostId)
		{
			var promotions = await _promotionRepo.GetAllByHostAsync(hostId);
			return promotions.Select(p => new PromotionDTO
			{
				PromotionId = p.PromotionId,
				Name = p.Name,
				StartDate = p.StartDate,
				EndDate = p.EndDate,
				DiscountPercentage = p.DiscountPercentage,
				TargetAudience = p.TargetAudience,
				Status = p.Status,
				CondotelId = p.CondotelId,
				CondotelName = p.Condotel?.Name
			});
		}
	}
}

