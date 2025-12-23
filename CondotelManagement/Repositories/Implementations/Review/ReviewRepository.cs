using CondotelManagement.Data;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CondotelManagement.Repositories
{
	public class ReviewRepository : IReviewRepository
	{
		private readonly CondotelDbVer1Context _db;
		public ReviewRepository(CondotelDbVer1Context db) => _db = db;

		public async Task<IEnumerable<Review>> GetReviewsByHostAsync(int hostId)
		{
			return await _db.Reviews
				.Include(r => r.Condotel)
				.Include(r => r.User)
				.Where(r => r.Condotel.HostId == hostId && r.Status != "Deleted") // Không hiển thị review đã bị xóa
				.ToListAsync();
		}

		public async Task<IEnumerable<Review>> GetReportedReviewsAsync()
		{
			return await _db.Reviews
				.Include(r => r.Condotel)
				.Include(r => r.User)
				.Where(r => r.Status == "Reported")
				.ToListAsync();
		}

		public async Task<Review?> GetByIdAsync(int id)
			=> await _db.Reviews.FindAsync(id);

		public async Task UpdateAsync(Review review)
		{
			_db.Reviews.Update(review);
			await _db.SaveChangesAsync();
		}
	}
}
