using CondotelManagement.Data;
using CondotelManagement.DTOs;
using CondotelManagement.Models;
using Google.Apis.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;

namespace CondotelManagement.Repositories
{
	public class UtilitiesRepository : IUtilitiesRepository
	{

		private readonly CondotelDbVer1Context _context;

		public UtilitiesRepository(CondotelDbVer1Context context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Utility>> GetAllAsync()
		{
			return await _context.Utilities
				.ToListAsync();
		}

		public async Task<Utility?> GetByIdAsync(int id)
		{
			return await _context.Utilities
				.FirstOrDefaultAsync(u => u.UtilityId == id);
		}

		public async Task<Utility> CreateAsync(Utility model)
		{
			_context.Utilities.Add(model);
			await _context.SaveChangesAsync();
			return model;
		}

		public async Task<bool> UpdateAsync(Utility model)
		{
			_context.Utilities.Update(model);
			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			var item = await _context.Utilities
				.FirstOrDefaultAsync(u => u.UtilityId == id);

			if (item == null) return false;

			_context.Utilities.Remove(item);
			return await _context.SaveChangesAsync() > 0;
		}

		public async Task<IEnumerable<Utility>> GetByResortAsync(int resortId)
		{
			return await _context.Utilities
				   .Where(u => u.ResortUtilities.Any(ru =>
					   ru.ResortId == resortId &&
					   ru.Status == "Active"
				   ))
				   .ToListAsync();
		}
	}
}
