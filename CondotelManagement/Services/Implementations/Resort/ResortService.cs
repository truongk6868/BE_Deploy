using CondotelManagement.DTOs;
using CondotelManagement.Models;
using CondotelManagement.Repositories;
using CondotelManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services
{
    public class ResortService : IResortService
    {
        private readonly IResortRepository _resortRepo;
        private readonly CondotelDbVer1Context _context;

        public ResortService(IResortRepository resortRepo, CondotelDbVer1Context context)
        {
            _resortRepo = resortRepo;
            _context = context;
        }

        public async Task<IEnumerable<ResortDTO>> GetAllAsync()
        {
            var resorts = await _resortRepo.GetAllAsync();
            return resorts.Select(r => new ResortDTO
            {
                ResortId = r.ResortId,
                LocationId = r.LocationId,
                Name = r.Name,
                Description = r.Description,
                Address = r.Address,
                Location = r.Location != null ? new LocationDTO
                {
                    LocationId = r.Location.LocationId,
                    Name = r.Location.Name,
                    Description = r.Location.Description
                } : null
            });
        }

        public async Task<ResortDTO?> GetByIdAsync(int id)
        {
            var resort = await _resortRepo.GetByIdAsync(id);
            if (resort == null) return null;

            return new ResortDTO
            {
                ResortId = resort.ResortId,
                LocationId = resort.LocationId,
                Name = resort.Name,
                Description = resort.Description,
                Address = resort.Address,
                Location = resort.Location != null ? new LocationDTO
                {
                    LocationId = resort.Location.LocationId,
                    Name = resort.Location.Name,
                    Description = resort.Location.Description
                } : null
            };
        }

        public async Task<IEnumerable<ResortDTO>> GetByLocationIdAsync(int locationId)
        {
            var resorts = await _resortRepo.GetByLocationIdAsync(locationId);
            return resorts.Select(r => new ResortDTO
            {
                ResortId = r.ResortId,
                LocationId = r.LocationId,
                Name = r.Name,
                Description = r.Description,
                Address = r.Address,
                Location = r.Location != null ? new LocationDTO
                {
                    LocationId = r.Location.LocationId,
                    Name = r.Location.Name,
                    Description = r.Location.Description
                } : null
            });
        }

        public async Task<ResortDTO> CreateAsync(ResortCreateUpdateDTO dto)
        {
            var resort = new Resort
            {
                LocationId = dto.LocationId,
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address
            };
            var created = await _resortRepo.AddAsync(resort);
            
            // Reload với Location để có đầy đủ thông tin
            var resortWithLocation = await _resortRepo.GetByIdAsync(created.ResortId);
            
            return new ResortDTO
            {
                ResortId = resortWithLocation!.ResortId,
                LocationId = resortWithLocation.LocationId,
                Name = resortWithLocation.Name,
                Description = resortWithLocation.Description,
                Address = resortWithLocation.Address,
                Location = resortWithLocation.Location != null ? new LocationDTO
                {
                    LocationId = resortWithLocation.Location.LocationId,
                    Name = resortWithLocation.Location.Name,
                    Description = resortWithLocation.Location.Description
                } : null
            };
        }

        public async Task<bool> UpdateAsync(int id, ResortCreateUpdateDTO dto)
        {
            var resort = await _resortRepo.GetByIdAsync(id);
            if (resort == null) return false;

            resort.LocationId = dto.LocationId;
            resort.Name = dto.Name;
            resort.Description = dto.Description;
            resort.Address = dto.Address;

            await _resortRepo.UpdateAsync(resort);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var resort = await _resortRepo.GetByIdAsync(id);
            if (resort == null) return false;

            await _resortRepo.DeleteAsync(resort);
            return true;
        }

        public async Task<bool> AddUtilityToResortAsync(int resortId, AddUtilityToResortDTO dto)
        {
            // Kiểm tra resort tồn tại
            var resort = await _resortRepo.GetByIdAsync(resortId);
            if (resort == null) return false;

            // Kiểm tra utility tồn tại
            var utility = await _context.Utilities.FindAsync(dto.UtilityId);
            if (utility == null) return false;

            // Kiểm tra đã có utility này trong resort chưa
            var existing = await _context.ResortUtilities
                .FirstOrDefaultAsync(ru => ru.ResortId == resortId && ru.UtilityId == dto.UtilityId);
            
            if (existing != null)
            {
                // Nếu đã tồn tại, cập nhật thông tin
                existing.Status = dto.Status;
                existing.OperatingHours = dto.OperatingHours;
                existing.Cost = dto.Cost;
                existing.DescriptionDetail = dto.DescriptionDetail;
                existing.MaximumCapacity = dto.MaximumCapacity;
                existing.DateAdded = DateOnly.FromDateTime(DateTime.UtcNow);
            }
            else
            {
                // Tạo mới ResortUtility
                var resortUtility = new ResortUtility
                {
                    ResortId = resortId,
                    UtilityId = dto.UtilityId,
                    Status = dto.Status,
                    OperatingHours = dto.OperatingHours,
                    Cost = dto.Cost,
                    DescriptionDetail = dto.DescriptionDetail,
                    MaximumCapacity = dto.MaximumCapacity,
                    DateAdded = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                _context.ResortUtilities.Add(resortUtility);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveUtilityFromResortAsync(int resortId, int utilityId)
        {
            // Kiểm tra resort tồn tại
            var resort = await _resortRepo.GetByIdAsync(resortId);
            if (resort == null) return false;

            // Tìm và xóa ResortUtility
            var resortUtility = await _context.ResortUtilities
                .FirstOrDefaultAsync(ru => ru.ResortId == resortId && ru.UtilityId == utilityId);
            
            if (resortUtility == null) return false;

            _context.ResortUtilities.Remove(resortUtility);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}










