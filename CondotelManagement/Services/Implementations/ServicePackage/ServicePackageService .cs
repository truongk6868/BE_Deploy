using CondotelManagement.DTOs;
using CondotelManagement.Models;
using CondotelManagement.Repositories;
using CondotelManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services
{
    public class ServicePackageService : IServicePackageService
    {
        private readonly IServicePackageRepository _repo;
        private readonly CondotelDbVer1Context _context;

        public ServicePackageService(IServicePackageRepository repo, CondotelDbVer1Context context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<IEnumerable<ServicePackageDTO>> GetAllByHostAsync(int hostId)
		{
            var data = await _repo.GetAllByHostAsync(hostId);
            return data.Select(x => new ServicePackageDTO
            {
                ServiceId = x.ServiceId,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Status = x.Status
            });
        }

        public async Task<ServicePackageDTO?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new ServicePackageDTO
            {
                ServiceId = x.ServiceId,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Status = x.Status
            };
        }

        public async Task<ServicePackageDTO> CreateAsync(int hostId, CreateServicePackageDTO dto)
        {
            var entity = new ServicePackage
            {
                HostID = hostId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Status = "Active"
            };

            await _repo.AddAsync(entity);
            await _repo.SaveAsync();

            return new ServicePackageDTO
            {
                ServiceId = entity.ServiceId,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                Status = entity.Status
            };
        }

        public async Task<ServicePackageDTO?> UpdateAsync(int id, UpdateServicePackageDTO dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return null;

            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.Price = dto.Price;
            entity.Status = dto.Status;

            await _repo.UpdateAsync(entity);
            await _repo.SaveAsync();

            return new ServicePackageDTO
            {
                ServiceId = entity.ServiceId,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                Status = entity.Status
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            // Soft delete
            entity.Status = "Inactive";
            await _repo.UpdateAsync(entity);
            await _repo.SaveAsync();
            return true;
        }

        public async Task<IEnumerable<ServicePackageDTO>> GetByCondotelAsync(int condotelId)
        {
            // Lấy condotel để biết HostId
            var condotel = await _context.Condotels
                .Include(c => c.Host)
                .FirstOrDefaultAsync(c => c.CondotelId == condotelId);

            if (condotel == null)
                return Enumerable.Empty<ServicePackageDTO>();

            // Lấy service packages của host
            var data = await _repo.GetAllByHostAsync(condotel.HostId);
            return data
                .Where(x => x.Status == "Active") // Chỉ lấy service packages active
                .Select(x => new ServicePackageDTO
                {
                    ServiceId = x.ServiceId,
                    Name = x.Name,
                    Description = x.Description,
                    Price = x.Price,
                    Status = x.Status
                });
        }
    }
}
