using CondotelManagement.Helpers;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Services.Interfaces.Admin;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/[controller]")]
    [Authorize(Roles = "Host")]
    public class ReportController : ControllerBase
    {
        private readonly IHostReportService _service;
        private readonly IHostService _hostService;
        private readonly IAdminReportService _adminReportService;
        private readonly IWebHostEnvironment _env;
        
        public ReportController(
            IHostReportService service, 
            IHostService hostService,
            IAdminReportService adminReportService,
            IWebHostEnvironment env)
        {
            _service = service;
            _hostService = hostService;
            _adminReportService = adminReportService;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetReport(
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to)
        {
            if (to < from)
                return BadRequest(ApiResponse<object>.Fail("Phạm vi ngày không hợp lệ"));

            //current host login
            var host = _hostService.GetByUserId(User.GetUserId());
            if (host == null)
				return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));

			var hostId = host.HostId;
            var result = await _service.GetReport(hostId, from, to);
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        // GET api/host/report/revenue?year=2024&month=1
        // Lấy báo cáo doanh thu theo tháng/năm
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            // Validate month nếu có
            if (month.HasValue && (month < 1 || month > 12))
            {
                return BadRequest(ApiResponse<object>.Fail("Tháng phải nằm trong khoảng từ 1 đến 12"));
            }

            // Validate year nếu có
            if (year.HasValue && (year < 2000 || year > 2100))
            {
                return BadRequest(ApiResponse<object>.Fail("Năm phải nằm trong khoảng từ 2000 đến 2100"));
            }

            // Validate: nếu có month thì phải có year
            if (month.HasValue && !year.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail("Năm là bắt buộc khi tháng được chỉ định"));
            }

            // Lấy hostId từ user đang đăng nhập
            var host = _hostService.GetByUserId(User.GetUserId());
            if (host == null)
                return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host. Vui lòng đăng ký làm host trước."));
            
            var hostId = host.HostId;
            var result = await _service.GetRevenueReportByMonthYear(hostId, year, month);
            
            // Debug: Log response structure
            Console.WriteLine($"[ReportController] Response - TotalRevenue: {result.TotalRevenue}, TotalBookings: {result.TotalBookings}");
            Console.WriteLine($"[ReportController] Response - MonthlyRevenues: {result.MonthlyRevenues?.Count ?? 0} items");
            Console.WriteLine($"[ReportController] Response - YearlyRevenues: {result.YearlyRevenues?.Count ?? 0} items");
            
            if (result.MonthlyRevenues != null && result.MonthlyRevenues.Any())
            {
                Console.WriteLine($"[ReportController] MonthlyRevenues details:");
                foreach (var m in result.MonthlyRevenues)
                {
                    Console.WriteLine($"  - {m.MonthName} {m.Year}: Revenue={m.Revenue}, Bookings={m.TotalBookings}, Completed={m.CompletedBookings}");
                }
            }

			return Ok(ApiResponse<object>.SuccessResponse(result));
		}

        /// <summary>
        /// Tải file báo cáo đã được tạo bởi admin
        /// GET /api/host/report/download/{reportId}
        /// </summary>
        [HttpGet("download/{reportId}")]
        public async Task<IActionResult> DownloadReport(int reportId)
        {
            try
            {
                // Lấy thông tin host hiện tại
                var host = _hostService.GetByUserId(User.GetUserId());
                if (host == null)
                    return Unauthorized(ApiResponse<object>.Fail("Không tìm thấy host"));

                // Lấy thông tin report từ admin report service
                var report = await _adminReportService.GetReportByIdAsync(reportId);
                
                if (report == null)
                    return NotFound(ApiResponse<object>.Fail("Không tìm thấy báo cáo"));

                // Kiểm tra report có thuộc về host này không
                if (!report.ReportType?.Contains($"HostReport_{host.HostId}") ?? true)
                    return StatusCode(403, ApiResponse<object>.Fail("Báo cáo này không thuộc về host của bạn"));

                // Lấy đường dẫn file
                if (string.IsNullOrEmpty(report.FileUrl))
                    return NotFound(ApiResponse<object>.Fail("File báo cáo không tồn tại"));

                var webRootPath = _env.WebRootPath ?? _env.ContentRootPath;
                var filePath = Path.Combine(webRootPath, report.FileUrl.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                    return NotFound(ApiResponse<object>.Fail("File báo cáo không tồn tại trên server"));

                // Trả về file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileName = report.FileName ?? $"report_{reportId}.xlsx";
                
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail($"Lỗi khi tải file: {ex.Message}"));
            }
        }
    }
}
