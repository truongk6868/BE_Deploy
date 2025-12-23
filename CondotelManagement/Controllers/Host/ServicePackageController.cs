using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/service-packages")]
    [Authorize(Roles = "Host")]
    public class ServicePackageController : ControllerBase
    {
        private readonly IServicePackageService _service;
		private readonly IHostService _hostService;
        private readonly CondotelDbVer1Context _context;

		public ServicePackageController(
            IServicePackageService service, 
            IHostService hostService,
            CondotelDbVer1Context context)
        {
            _service = service;
            _hostService = hostService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllByHost()
        {
			//current host login
			var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
			return Ok(ApiResponse<object>.SuccessResponse(await _service.GetAllByHostAsync(hostId)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateServicePackageDTO dto)
        {
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}
			var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
            var created = await _service.CreateAsync(hostId, dto);
			return Ok(ApiResponse<object>.SuccessResponse(created, "Đã tạo thành công"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateServicePackageDTO dto)
        {
			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}
			var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
			return Ok(ApiResponse<object>.SuccessResponse(result, "Đã sửa thành công"));
		}

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Lấy host hiện tại
                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
                    return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host"));

                // Kiểm tra service package có tồn tại và thuộc về host này không
                var servicePackage = await _service.GetByIdAsync(id);
                if (servicePackage == null)
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy gói dịch vụ"));

                // Kiểm tra ownership - cần lấy HostID từ entity
                var servicePackageEntity = await _context.ServicePackages.FindAsync(id);
                if (servicePackageEntity == null)
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy gói dịch vụ"));

                if (servicePackageEntity.HostID != host.HostId)
                    return StatusCode(403, ApiResponse<object>.Fail("Bạn không có quyền xóa gói dịch vụ này"));

                // Kiểm tra xem service package có đang được sử dụng trong booking không
                var isUsed = await _context.BookingDetails
                    .AnyAsync(bd => bd.ServiceId == id);

                if (isUsed)
                {
                    // Nếu đang được sử dụng, chỉ cho phép soft delete (set Status = "Inactive")
                    var success = await _service.DeleteAsync(id);
                    if (!success)
                        return BadRequest(ApiResponse<object>.Fail("Không thể xóa gói dịch vụ"));

                    return Ok(ApiResponse<object>.SuccessResponse(null, "Gói dịch vụ đã được vô hiệu hóa (đang được sử dụng trong booking)"));
                }

                // Nếu không được sử dụng, thực hiện soft delete
                var deleteSuccess = await _service.DeleteAsync(id);
                if (!deleteSuccess)
                    return BadRequest(ApiResponse<object>.Fail("Không thể xóa gói dịch vụ"));

                return Ok(ApiResponse<object>.SuccessResponse(null, "Xóa thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail($"Lỗi khi xóa gói dịch vụ: {ex.Message}"));
            }
        }
    }
}
