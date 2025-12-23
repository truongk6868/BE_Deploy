using CondotelManagement.DTOs.Admin;
using CondotelManagement.Helpers;
using CondotelManagement.Services.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CondotelManagement.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportController : ControllerBase
    {
        private readonly IAdminReportService _service;

        public AdminReportController(IAdminReportService service)
        {
            _service = service;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid");
            if (claim == null || !int.TryParse(claim.Value, out int userId))
                throw new UnauthorizedAccessException("Invalid user ID");
            return userId;
        }

        /// <summary>
        /// Tạo báo cáo cho host
        /// POST /api/admin/reports
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] AdminReportCreateDTO? dto)
        {
            try
            {
                // Kiểm tra DTO null
                if (dto == null)
                {
                    return BadRequest(ApiResponse<object>.Fail("Request body không được để trống"));
                }

                // Log để debug
                Console.WriteLine($"[AdminReportController] Received DTO: HostId={dto.HostId}, ReportType={dto.ReportType}, FromDate={dto.FromDate}, ToDate={dto.ToDate}, Year={dto.Year}, Month={dto.Month}");

                // Kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    
                    Console.WriteLine($"[AdminReportController] ModelState errors: {string.Join(", ", errors.SelectMany(e => e.Value))}");
                    return BadRequest(ApiResponse<object>.Fail("Dữ liệu không hợp lệ", errors));
                }

                // Validate dữ liệu
                // HostId là optional - nếu null hoặc <= 0 thì sẽ tạo báo cáo cho tất cả hosts
                // Nếu có HostId thì phải > 0
                if (dto.HostId.HasValue && dto.HostId.Value <= 0)
                {
                    return BadRequest(ApiResponse<object>.Fail("HostId phải lớn hơn 0 nếu được chỉ định"));
                }

                // Validate date range nếu có
                if (dto.FromDate.HasValue && dto.ToDate.HasValue && dto.ToDate < dto.FromDate)
                {
                    return BadRequest(ApiResponse<object>.Fail("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu"));
                }

                // Validate month nếu có
                if (dto.Month.HasValue && (dto.Month < 1 || dto.Month > 12))
                {
                    return BadRequest(ApiResponse<object>.Fail("Tháng phải nằm trong khoảng từ 1 đến 12"));
                }

                // Validate year nếu có
                if (dto.Year.HasValue && (dto.Year < 2000 || dto.Year > 2100))
                {
                    return BadRequest(ApiResponse<object>.Fail("Năm phải nằm trong khoảng từ 2000 đến 2100"));
                }

                // Validate: nếu có month thì phải có year
                if (dto.Month.HasValue && !dto.Year.HasValue)
                {
                    return BadRequest(ApiResponse<object>.Fail("Năm là bắt buộc khi tháng được chỉ định"));
                }

                var adminId = GetCurrentUserId();
                var result = await _service.CreateHostReportAsync(adminId, dto);
                return Ok(ApiResponse<AdminReportResponseDTO>.SuccessResponse(result, "Báo cáo đã được tạo thành công"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail($"Lỗi khi tạo báo cáo: {ex.Message}"));
            }
        }

        /// <summary>
        /// Lấy thông tin báo cáo theo ID
        /// GET /api/admin/reports/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReport(int id)
        {
            var report = await _service.GetReportByIdAsync(id);
            if (report == null)
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy báo cáo"));

            return Ok(ApiResponse<AdminReportResponseDTO>.SuccessResponse(report));
        }

        /// <summary>
        /// Lấy tất cả báo cáo
        /// GET /api/admin/reports
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _service.GetAllReportsAsync();
            return Ok(ApiResponse<object>.SuccessResponse(reports));
        }

        /// <summary>
        /// Lấy báo cáo theo admin ID
        /// GET /api/admin/reports/admin/{adminId}
        /// </summary>
        [HttpGet("admin/{adminId}")]
        public async Task<IActionResult> GetReportsByAdmin(int adminId)
        {
            var reports = await _service.GetReportsByAdminIdAsync(adminId);
            return Ok(ApiResponse<object>.SuccessResponse(reports));
        }

        /// <summary>
        /// Lấy báo cáo theo host ID
        /// GET /api/admin/reports/host/{hostId}
        /// </summary>
        [HttpGet("host/{hostId}")]
        public async Task<IActionResult> GetReportsByHost(int hostId)
        {
            var reports = await _service.GetReportsByHostIdAsync(hostId);
            return Ok(ApiResponse<object>.SuccessResponse(reports));
        }

        /// <summary>
        /// Xóa báo cáo
        /// DELETE /api/admin/reports/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var result = await _service.DeleteReportAsync(id);
            if (!result)
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy báo cáo"));

            return Ok(ApiResponse<object>.SuccessResponse(null, "Đã xóa báo cáo thành công"));
        }

        /// <summary>
        /// Lấy danh sách tất cả hosts để chọn khi tạo báo cáo
        /// GET /api/admin/reports/hosts
        /// </summary>
        [HttpGet("hosts")]
        public async Task<IActionResult> GetAllHosts()
        {
            try
            {
                var hosts = await _service.GetAllHostsAsync();
                return Ok(ApiResponse<object>.SuccessResponse(hosts));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail($"Lỗi khi lấy danh sách hosts: {ex.Message}"));
            }
        }
    }
}

