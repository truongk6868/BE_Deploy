using CondotelManagement.DTOs;
using CondotelManagement.Models;
using CondotelManagement.Repositories;
using Google.Apis.Util;
using Microsoft.Extensions.Hosting;

namespace CondotelManagement.Services 
{
	public class UtilitiesService : IUtilitiesService
	{
		private readonly IUtilitiesRepository _repository;

		public UtilitiesService(IUtilitiesRepository repo)
		{
			_repository = repo;
		}

		public async Task<IEnumerable<UtilityResponseDTO>> GetAllAsync()
		{
			var utilities = await _repository.GetAllAsync();

			return utilities.Select(u => new UtilityResponseDTO
			{
				UtilityId = u.UtilityId,
				Name = u.Name,
				Category = u.Category,
				Description = u.Description
			});
		}

		// GET BY ID
		public async Task<UtilityResponseDTO?> GetByIdAsync(int id)
		{
			var utility = await _repository.GetByIdAsync(id);

			if (utility == null)
				return null;

			return new UtilityResponseDTO
			{
				UtilityId = utility.UtilityId,
				Name = utility.Name,
				Category = utility.Category,
				Description = utility.Description
			};
		}

		// CREATE NEW UTILITY
		public async Task<UtilityResponseDTO> CreateAsync(UtilityRequestDTO dto)
		{
			var utility = new Utility
			{
				Name = dto.Name,
				Description = dto.Description,
				Category = dto.Category
			};

			var created = await _repository.CreateAsync(utility);

			return new UtilityResponseDTO
			{
				UtilityId = created.UtilityId,
				Name = created.Name,
				Description = created.Description,
				Category = created.Category
			};
		}

		// UPDATE
		public async Task<bool> UpdateAsync(int id, UtilityRequestDTO dto)
		{
			var entity = await _repository.GetByIdAsync(id);

			if (entity == null)
				return false;

			entity.Name = dto.Name;
			entity.Description = dto.Description;
			entity.Category = dto.Category;

			return await _repository.UpdateAsync(entity);
		}

		// DELETE
		public async Task<bool> DeleteAsync(int id)
		{
			return await _repository.DeleteAsync(id);
		}

		public async Task<IEnumerable<UtilityResponseDTO>> GetByResortAsync(int resortId)
		{
			var utilities = await _repository.GetByResortAsync(resortId);
			return utilities.Select(u => new UtilityResponseDTO
			{
				UtilityId = u.UtilityId,
				Name = u.Name,
				Category = u.Category,
				Description = u.Description
			});
		}
	}
}
