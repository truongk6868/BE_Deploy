using CondotelManagement.Data;
using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/[controller]")]
    [Authorize(Roles = "Host")]
    public class CondotelController : ControllerBase
    {
        private readonly ICondotelService _condotelService;
        private readonly IHostService _hostService;
        private readonly IPackageFeatureService _featureService; // THÊM DÒNG NÀY
        private readonly CondotelDbVer1Context _context;

        public CondotelController(ICondotelService condotelService, IHostService hostService, IPackageFeatureService featureService,CondotelDbVer1Context context)
        {
            _condotelService = condotelService;
            _hostService = hostService;
            _featureService = featureService;
            _context = context;
        }

        //GET /api/host/condotel?pageNumber=1&pageSize=10
        [HttpGet]
        public ActionResult<object> GetAllCondotelByHost([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            //current host login
            var host = _hostService.GetByUserId(User.GetUserId());
            if (host == null)
                return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));
            
            // Validate pagination parameters
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 10; // Default 10, max 100

            var hostId = host.HostId;
            var result = _condotelService.GetCondtelsByHostPaged(hostId, pageNumber, pageSize);
            
            return Ok(new
            {
                success = true,
                data = result.Items,
                pagination = new
                {
                    pageNumber = result.PageNumber,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages,
                    hasPreviousPage = result.HasPreviousPage,
                    hasNextPage = result.HasNextPage
                }
            });
        }

        //GET /api/condotel/{id}
        [HttpGet("{id}")]
        public ActionResult<CondotelDetailDTO> GetById(int id)
        {
            var condotel = _condotelService.GetCondotelById(id);
            if (condotel == null)
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy căn hộ khách sạn"));

            return Ok(ApiResponse<object>.SuccessResponse(condotel));
        }

        //POST /api/condotel
        [HttpPost]
        public ActionResult Create([FromBody] CondotelCreateDTO condotelDto)
        {
            if (condotelDto == null)
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu condotel không hợp lệ"));

			if (condotelDto.Prices != null && condotelDto.Prices.Count > 0)
			{
				for (int i = 0; i < condotelDto.Prices.Count; i++)
				{
					var price = condotelDto.Prices[i];

					// Check Start < End
					if (price.StartDate >= price.EndDate)
					{
						ModelState.AddModelError($"Prices[{i}].StartDate", "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");
						ModelState.AddModelError($"Prices[{i}].EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
					}
				}
			}

			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}

			try
            {
                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
                    return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));

                // LẤY GÓI HIỆN TẠI CỦA HOST
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var activePackage = _context.HostPackages
                    .Include(hp => hp.Package)
                    .Where(hp => hp.HostId == host.HostId
                        && hp.Status == "Active"
                        && hp.StartDate <= today
                        && hp.EndDate >= today)
                    .OrderByDescending(hp => hp.StartDate)
                    .FirstOrDefault();//chuẩn 

                var maxListings = activePackage != null
    ? _featureService.GetMaxListingCount(activePackage.PackageId)
    : 0;

                // ĐẾM SỐ CONDOTEL HIỆN TẠI CỦA HOST
                var currentCount = _context.Condotels
                    .Count(c => c.HostId == host.HostId && c.Status != "Deleted");
                if (currentCount >= maxListings)
                {
                    string message = maxListings == 0
                        ? "Bạn chưa có gói dịch vụ nào. Vui lòng mua gói để đăng tin."
                        : $"Bạn đã đạt giới hạn đăng tin ({currentCount}/{maxListings}). Vui lòng nâng cấp gói để đăng thêm.";

                    return StatusCode(403, new
                    {
                        success = false,
                        message,
                        currentCount,
                        maxListings,
                        upgradeRequired = true
                    });
                }
                // KIỂM TRA GIỚI HẠN
                if (currentCount >= maxListings)
                {
                    return StatusCode(403, new
                    {
                        message = $"Bạn đã đạt giới hạn đăng tin. Gói hiện tại chỉ cho phép tối đa {maxListings} condotel.",
                        currentCount,
                        maxListings,
                        upgradeRequired = maxListings == 0 || maxListings < 10 // gợi ý nâng cấp
                    });
                }

                condotelDto.HostId = host.HostId;
                var created = _condotelService.CreateCondotel(condotelDto);

				return Ok(ApiResponse<object>.SuccessResponse(created, "Tạo condotel thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Lỗi hệ thống: " + ex.Message));
            }
        }

        //PUT /api/condotel/{id}
        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] CondotelUpdateDTO condotelDto)
        {
            if (condotelDto == null)
				return BadRequest(ApiResponse<object>.Fail("Dữ liệu condotel không hợp lệ"));

			if (condotelDto.CondotelId != id)
                return BadRequest(ApiResponse<object>.Fail("ID Condotel không khớp"));

			if (condotelDto.Prices != null && condotelDto.Prices.Count > 0)
			{
				for (int i = 0; i < condotelDto.Prices.Count; i++)
				{
					var price = condotelDto.Prices[i];

					// Check Start < End
					if (price.StartDate >= price.EndDate)
					{
						ModelState.AddModelError($"Prices[{i}].StartDate", "Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");
						ModelState.AddModelError($"Prices[{i}].EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
					}
				}
			}

			// Validate DataAnnotation
			if (!ModelState.IsValid)
			{
				return BadRequest(ApiResponse<object>.Fail(ModelState.ToErrorDictionary()));
			}

			try
            {
                // Get current host from authenticated user
                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
					return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));

				// Kiểm tra ownership - đảm bảo condotel thuộc về host này
				var existingCondotel = _condotelService.GetCondotelById(id);
                if (existingCondotel == null)
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy condotel"));

                if (existingCondotel.HostId != host.HostId)
                    return StatusCode(403, ApiResponse<object>.Fail("Bạn không có quyền cập nhật căn hộ này"));

                // Set HostId từ authenticated user (không cho client set)
                condotelDto.HostId = host.HostId;

                var updated = _condotelService.UpdateCondotel(condotelDto);
                if (updated == null)
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy condotel"));
				return Ok(ApiResponse<object>.SuccessResponse(updated, "Condotel đã được cập nhật thành công"));
            }
            catch (ArgumentNullException ex)
            {
				return BadRequest(ApiResponse<object>.Fail(ex.Message));
			}
            catch (ArgumentException ex)
            {
				return BadRequest(ApiResponse<object>.Fail(ex.Message));
			}
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
				return BadRequest(ApiResponse<object>.Fail(ex.Message));
			}
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Lỗi hệ thống: " + ex.Message));
            }
        }

        //DELETE /api/condotel/{id}
        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                // Get current host from authenticated user
                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
					return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));

				// Kiểm tra ownership - đảm bảo condotel thuộc về host này
				var existingCondotel = _condotelService.GetCondotelById(id);
                if (existingCondotel == null)
					return NotFound(ApiResponse<object>.Fail("Không tìm thấy condotel"));

				if (existingCondotel.HostId != host.HostId)
                    return StatusCode(403, ApiResponse<object>.Fail("Bạn không có quyền xóa căn hộ này"));

                try
                {
                    var success = _condotelService.DeleteCondotel(id);
                    if (!success)
                        return NotFound(ApiResponse<object>.Fail("Condotel không tìm thấy hoặc đã bị xóa"));

                    return Ok(ApiResponse<object>.SuccessResponse("Condotel đã inactive thành công"));
                }
                catch (InvalidOperationException ex)
                {
                    // Lỗi nghiệp vụ: có booking active
                    return BadRequest(ApiResponse<object>.Fail(ex.Message));
                }
            }
            catch (Exception ex)
            {
				return StatusCode(500, ApiResponse<object>.Fail("Lỗi hệ thống: " + ex.Message));
			}
        }

        //GET /api/host/condotel/inactive?pageNumber=1&pageSize=10
        // Lấy danh sách các condotel không hoạt động (Inactive)
        [HttpGet("inactive")]
        public ActionResult<object> GetInactiveCondotels([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
                    return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));

                // Validate pagination parameters
                if (pageNumber < 1)
                    pageNumber = 1;
                if (pageSize < 1 || pageSize > 100)
                    pageSize = 10; // Default 10, max 100

                var hostId = host.HostId;
                var result = _condotelService.GetInactiveCondotelsByHostPaged(hostId, pageNumber, pageSize);

                return Ok(new
                {
                    success = true,
                    message = "Danh sách condotel không hoạt động",
                    data = result.Items,
                    pagination = new
                    {
                        pageNumber = result.PageNumber,
                        pageSize = result.PageSize,
                        totalCount = result.TotalCount,
                        totalPages = result.TotalPages,
                        hasPreviousPage = result.HasPreviousPage,
                        hasNextPage = result.HasNextPage
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Lỗi hệ thống: " + ex.Message));
            }
        }

        //PUT /api/host/condotel/{id}/activate
        // Kích hoạt lại condotel (chuyển từ Inactive sang Active)
        [HttpPut("{id}/activate")]
        public ActionResult ActivateCondotel(int id)
        {
            try
            {
                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
                    return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));

                // Kiểm tra ownership - đảm bảo condotel thuộc về host này
                var existingCondotel = _condotelService.GetCondotelById(id);
                if (existingCondotel == null)
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy condotel"));

                if (existingCondotel.HostId != host.HostId)
                    return StatusCode(403, ApiResponse<object>.Fail("Bạn không có quyền kích hoạt căn hộ này"));

                // Kiểm tra xem condotel có status là Inactive không
                if (existingCondotel.Status != "Inactive")
                    return BadRequest(ApiResponse<object>.Fail($"Chỉ có thể kích hoạt lại condotel có trạng thái Inactive. Trạng thái hiện tại: {existingCondotel.Status}"));

                var success = _condotelService.ActivateCondotel(id);
                if (!success)
                    return StatusCode(500, ApiResponse<object>.Fail("Không thể kích hoạt condotel"));

                return Ok(ApiResponse<object>.SuccessResponse("Condotel đã được kích hoạt thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Lỗi hệ thống: " + ex.Message));
            }
        }
    }
}
