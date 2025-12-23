using ClosedXML.Excel;
using CondotelManagement.Data;
using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Admin;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Admin;
using CondotelManagement.Services;
using CondotelManagement.Services.Interfaces.Admin;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Services.Implementations.Admin
{
    public class AdminReportService : IAdminReportService
    {
        private readonly IAdminReportRepository _reportRepo;
        private readonly IHostReportService _hostReportService;
        private readonly CondotelDbVer1Context _context;
        private readonly IWebHostEnvironment _env;

        public AdminReportService(
            IAdminReportRepository reportRepo,
            IHostReportService hostReportService,
            CondotelDbVer1Context context,
            IWebHostEnvironment env)
        {
            _reportRepo = reportRepo;
            _hostReportService = hostReportService;
            _context = context;
            _env = env;
        }

        public async Task<AdminReportResponseDTO> CreateHostReportAsync(int adminId, AdminReportCreateDTO dto)
        {
            // Xác định loại report
            bool isAllHostsReport = !dto.HostId.HasValue || dto.HostId.Value <= 0;
            
            // Tạo file Excel
            var excelBytes = await GenerateExcelReportAsync(
                isAllHostsReport ? null : dto.HostId.Value,
                dto.FromDate,
                dto.ToDate,
                dto.Year,
                dto.Month);

            if (excelBytes == null)
                throw new Exception("Failed to generate Excel report");

            // Lưu file vào wwwroot/reports
            var reportsFolder = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "reports");
            if (!Directory.Exists(reportsFolder))
                Directory.CreateDirectory(reportsFolder);

            var fileName = isAllHostsReport 
                ? $"AllHostsReport_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx"
                : $"HostReport_{dto.HostId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            var filePath = Path.Combine(reportsFolder, fileName);
            await File.WriteAllBytesAsync(filePath, excelBytes);

            // Tạo URL để truy cập file
            var fileUrl = $"/reports/{fileName}";

            // Lưu vào database
            var report = new AdminReport
            {
                AdminId = adminId,
                ReportType = isAllHostsReport ? "AllHostsReport" : $"HostReport_{dto.HostId}",
                GeneratedDate = DateTime.UtcNow,
                FileUrl = fileUrl
            };

            var createdReport = await _reportRepo.CreateReportAsync(report);

            // Lấy thông tin admin
            var admin = await _context.Users.FindAsync(adminId);

            return new AdminReportResponseDTO
            {
                ReportId = createdReport.ReportId,
                AdminId = createdReport.AdminId,
                AdminName = admin?.FullName,
                ReportType = createdReport.ReportType,
                GeneratedDate = createdReport.GeneratedDate,
                FileUrl = createdReport.FileUrl,
                FileName = fileName
            };
        }

        public async Task<AdminReportResponseDTO?> GetReportByIdAsync(int reportId)
        {
            var report = await _reportRepo.GetReportByIdAsync(reportId);
            if (report == null) return null;

            return new AdminReportResponseDTO
            {
                ReportId = report.ReportId,
                AdminId = report.AdminId,
                AdminName = report.Admin?.FullName,
                ReportType = report.ReportType,
                GeneratedDate = report.GeneratedDate,
                FileUrl = report.FileUrl,
                FileName = report.FileUrl?.Split('/').LastOrDefault()
            };
        }

        public async Task<IEnumerable<AdminReportListDTO>> GetAllReportsAsync()
        {
            var reports = await _reportRepo.GetAllReportsAsync();
            var result = reports.Select(r => MapToDTO(r)).ToList();
            
            // Load host names
            foreach (var dto in result)
            {
                if (dto.HostId.HasValue)
                {
                    dto.HostName = await GetHostName(dto.HostId.Value);
                }
            }
            
            return result;
        }

        public async Task<IEnumerable<AdminReportListDTO>> GetReportsByAdminIdAsync(int adminId)
        {
            var reports = await _reportRepo.GetReportsByAdminIdAsync(adminId);
            var result = reports.Select(r => MapToDTO(r)).ToList();
            
            // Load host names
            foreach (var dto in result)
            {
                if (dto.HostId.HasValue)
                {
                    dto.HostName = await GetHostName(dto.HostId.Value);
                }
            }
            
            return result;
        }

        public async Task<IEnumerable<AdminReportListDTO>> GetReportsByHostIdAsync(int hostId)
        {
            var reports = await _reportRepo.GetReportsByHostIdAsync(hostId);
            var result = reports.Select(r => MapToDTO(r)).ToList();
            
            // Load host name
            var hostName = await GetHostName(hostId);
            foreach (var dto in result)
            {
                dto.HostName = hostName;
            }
            
            return result;
        }

        public async Task<bool> DeleteReportAsync(int reportId)
        {
            var report = await _reportRepo.GetReportByIdAsync(reportId);
            if (report == null) return false;

            // Xóa file nếu tồn tại
            if (!string.IsNullOrEmpty(report.FileUrl))
            {
                var filePath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, report.FileUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            return await _reportRepo.DeleteReportAsync(reportId);
        }

        public async Task<byte[]?> GenerateExcelReportAsync(int? hostId, DateOnly? from, DateOnly? to, int? year, int? month)
        {
            try
            {
                // Lấy dữ liệu report
                HostReportDTO? reportData = null;
                RevenueReportResponseDTO? revenueData = null;
                bool isAllHostsReport = !hostId.HasValue || hostId.Value <= 0;

                if (isAllHostsReport)
                {
                    // Báo cáo cho tất cả hosts
                    revenueData = await GetAllHostsRevenueReportAsync(year, month);
                }
                else if (year.HasValue || month.HasValue)
                {
                    // Báo cáo doanh thu theo tháng/năm cho host cụ thể
                    revenueData = await _hostReportService.GetRevenueReportByMonthYear(hostId.Value, year, month);
                }
                else
                {
                    // Báo cáo tổng hợp cho host cụ thể
                    reportData = await _hostReportService.GetReport(hostId.Value, from, to);
                }

                // Lấy thông tin host (nếu là report cho host cụ thể)
                Models.Host? host = null;
                if (!isAllHostsReport)
                {
                    host = await _context.Hosts
                        .Include(h => h.User)
                        .FirstOrDefaultAsync(h => h.HostId == hostId!.Value);

                    if (host == null)
                        return null;
                }

                // Tạo Excel workbook
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Báo cáo");

                // Header
                worksheet.Cell(1, 1).Value = isAllHostsReport ? "BÁO CÁO DOANH THU TẤT CẢ HOSTS" : "BÁO CÁO DOANH THU";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 5).Merge();

                if (!isAllHostsReport)
                {
                    worksheet.Cell(2, 1).Value = "Host:";
                    worksheet.Cell(2, 2).Value = host!.User?.FullName ?? "N/A";
                }
                else
                {
                    worksheet.Cell(2, 1).Value = "Phạm vi:";
                    worksheet.Cell(2, 2).Value = "Tất cả hosts";
                }
                
                worksheet.Cell(3, 1).Value = "Ngày tạo:";
                worksheet.Cell(3, 2).Value = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss");

                int row = 5;

                if (revenueData != null)
                {
                    // Revenue Report Data
                    worksheet.Cell(row, 1).Value = "Tổng doanh thu:";
                    worksheet.Cell(row, 2).Value = revenueData.TotalRevenue.ToString("N0") + " ₫";
                    row++;

                    worksheet.Cell(row, 1).Value = "Tổng đặt phòng:";
                    worksheet.Cell(row, 2).Value = revenueData.TotalBookings;
                    row++;

                    worksheet.Cell(row, 1).Value = "Đã hoàn thành:";
                    worksheet.Cell(row, 2).Value = revenueData.CompletedBookings;
                    row++;

                    worksheet.Cell(row, 1).Value = "Đã hủy:";
                    worksheet.Cell(row, 2).Value = revenueData.CancelledBookings;
                    row += 2;

                    // Monthly Revenues
                    if (revenueData.MonthlyRevenues != null && revenueData.MonthlyRevenues.Any())
                    {
                        worksheet.Cell(row, 1).Value = "DOANH THU THEO THÁNG";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        row++;

                        worksheet.Cell(row, 1).Value = "Tháng";
                        worksheet.Cell(row, 2).Value = "Doanh thu";
                        worksheet.Cell(row, 3).Value = "Tổng đặt phòng";
                        worksheet.Cell(row, 4).Value = "Đã hoàn thành";
                        worksheet.Cell(row, 5).Value = "Đã hủy";
                        worksheet.Range(row, 1, row, 5).Style.Font.Bold = true;
                        row++;

                        foreach (var monthly in revenueData.MonthlyRevenues)
                        {
                            worksheet.Cell(row, 1).Value = monthly.MonthName;
                            worksheet.Cell(row, 2).Value = monthly.Revenue.ToString("N0") + " ₫";
                            worksheet.Cell(row, 3).Value = monthly.TotalBookings;
                            worksheet.Cell(row, 4).Value = monthly.CompletedBookings;
                            worksheet.Cell(row, 5).Value = monthly.CancelledBookings;
                            row++;
                        }
                    }
                }
                else if (reportData != null)
                {
                    // Host Report Data
                    worksheet.Cell(row, 1).Value = "Doanh thu:";
                    worksheet.Cell(row, 2).Value = reportData.Revenue.ToString("N0") + " ₫";
                    row++;

                    worksheet.Cell(row, 1).Value = "Tổng phòng:";
                    worksheet.Cell(row, 2).Value = reportData.TotalRooms;
                    row++;

                    worksheet.Cell(row, 1).Value = "Phòng đã đặt:";
                    worksheet.Cell(row, 2).Value = reportData.RoomsBooked;
                    row++;

                    worksheet.Cell(row, 1).Value = "Tỷ lệ lấp đầy:";
                    worksheet.Cell(row, 2).Value = reportData.OccupancyRate.ToString("F2") + "%";
                    row++;

                    worksheet.Cell(row, 1).Value = "Tổng đặt phòng:";
                    worksheet.Cell(row, 2).Value = reportData.TotalBookings;
                    row++;

                    worksheet.Cell(row, 1).Value = "Đã hoàn thành:";
                    worksheet.Cell(row, 2).Value = reportData.CompletedBookings;
                    row++;

                    worksheet.Cell(row, 1).Value = "Tổng khách hàng:";
                    worksheet.Cell(row, 2).Value = reportData.TotalCustomers;
                    row++;

                    worksheet.Cell(row, 1).Value = "Giá trị trung bình:";
                    worksheet.Cell(row, 2).Value = reportData.AverageBookingValue.ToString("N0") + " ₫";
                    row++;

                    worksheet.Cell(row, 1).Value = "Đang xử lý:";
                    worksheet.Cell(row, 2).Value = reportData.PendingBookings;
                    row++;

                    worksheet.Cell(row, 1).Value = "Đã xác nhận:";
                    worksheet.Cell(row, 2).Value = reportData.ConfirmedBookings;
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Convert to byte array
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating Excel report: {ex.Message}");
                return null;
            }
        }

        private AdminReportListDTO MapToDTO(AdminReport report)
        {
            var hostId = ExtractHostIdFromReportType(report.ReportType);
            return new AdminReportListDTO
            {
                ReportId = report.ReportId,
                AdminId = report.AdminId,
                AdminName = report.Admin?.FullName,
                ReportType = report.ReportType,
                GeneratedDate = report.GeneratedDate,
                FileUrl = report.FileUrl,
                FileName = report.FileUrl?.Split('/').LastOrDefault(),
                HostId = hostId,
                HostName = null // Sẽ được load riêng nếu cần
            };
        }

        private int? ExtractHostIdFromReportType(string? reportType)
        {
            if (string.IsNullOrEmpty(reportType) || !reportType.Contains("HostReport"))
                return null;

            var parts = reportType.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[1], out int hostId))
                return hostId;

            return null;
        }

        private async Task<string?> GetHostName(int hostId)
        {
            var host = await _context.Hosts
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.HostId == hostId);
            return host?.User?.FullName;
        }

        public async Task<RevenueReportResponseDTO> GetAllHostsRevenueReportAsync(int? year, int? month)
        {
            // Lấy tất cả bookings (không filter theo host)
            var allBookingsQuery = _context.Bookings.AsQueryable();

            // Filter theo năm nếu có
            if (year.HasValue)
            {
                allBookingsQuery = allBookingsQuery.Where(b => b.EndDate.Year == year.Value);
            }

            // Filter theo tháng nếu có (chỉ filter khi đã có year)
            if (month.HasValue && year.HasValue)
            {
                allBookingsQuery = allBookingsQuery.Where(b => b.EndDate.Month == month.Value);
            }

            var allBookings = await allBookingsQuery.ToListAsync();

            // Helper method để check status "Completed"
            bool IsCompletedStatus(string? status)
            {
                if (string.IsNullOrWhiteSpace(status)) return false;
                var trimmedStatus = status.Trim();
                return trimmedStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                       trimmedStatus.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase);
            }

            // Helper method để check status "Cancelled"
            bool IsCancelledStatus(string? status)
            {
                if (string.IsNullOrWhiteSpace(status)) return false;
                var trimmedStatus = status.Trim();
                return trimmedStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                       trimmedStatus.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase) ||
                       trimmedStatus.Equals("Canceled", StringComparison.OrdinalIgnoreCase);
            }

            // Filter Completed bookings
            var completedBookings = allBookings
                .Where(b => IsCompletedStatus(b.Status))
                .ToList();

            // Tính tổng doanh thu (CHỈ TỪ BOOKING COMPLETED)
            var totalRevenue = completedBookings
                .Sum(b => b.TotalPrice ?? 0m);

            // Tính tổng số booking (tất cả status)
            var totalBookings = allBookings.Count;

            // Tính số booking Completed
            var completedBookingsCount = completedBookings.Count;

            // Tính số booking Cancelled
            var cancelledBookings = allBookings.Count(b => IsCancelledStatus(b.Status));

            // Nhóm theo tháng/năm
            var monthlyData = allBookings
                .GroupBy(b => new { b.EndDate.Year, b.EndDate.Month })
                .Select(g => new MonthlyRevenueDTO
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = $"Tháng {g.Key.Month}",
                    Revenue = g.Where(b => IsCompletedStatus(b.Status))
                        .Sum(b => b.TotalPrice ?? 0m),
                    TotalBookings = g.Count(),
                    CompletedBookings = g.Count(b => IsCompletedStatus(b.Status)),
                    CancelledBookings = g.Count(b => IsCancelledStatus(b.Status))
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            // Nhóm theo năm
            var yearlyData = allBookings
                .GroupBy(b => b.EndDate.Year)
                .Select(g => new YearlyRevenueDTO
                {
                    Year = g.Key,
                    TotalRevenue = g.Where(b => IsCompletedStatus(b.Status))
                        .Sum(b => b.TotalPrice ?? 0m),
                    TotalBookings = g.Count(),
                    CompletedBookings = g.Count(b => IsCompletedStatus(b.Status)),
                    CancelledBookings = g.Count(b => IsCancelledStatus(b.Status)),
                    MonthlyData = g
                        .GroupBy(b => b.EndDate.Month)
                        .Select(mg => new MonthlyRevenueDTO
                        {
                            Year = g.Key,
                            Month = mg.Key,
                            MonthName = $"Tháng {mg.Key}",
                            Revenue = mg.Where(b => IsCompletedStatus(b.Status))
                                .Sum(b => b.TotalPrice ?? 0m),
                            TotalBookings = mg.Count(),
                            CompletedBookings = mg.Count(b => IsCompletedStatus(b.Status)),
                            CancelledBookings = mg.Count(b => IsCancelledStatus(b.Status))
                        })
                        .OrderBy(m => m.Month)
                        .ToList()
                })
                .OrderBy(y => y.Year)
                .ToList();

            return new RevenueReportResponseDTO
            {
                TotalRevenue = Math.Round(totalRevenue, 2),
                TotalBookings = totalBookings,
                CompletedBookings = completedBookingsCount,
                CancelledBookings = cancelledBookings,
                MonthlyRevenues = monthlyData,
                YearlyRevenues = yearlyData
            };
        }

        public async Task<IEnumerable<HostListItemDTO>> GetAllHostsAsync()
        {
            var hosts = await _context.Hosts
                .Include(h => h.User)
                .Where(h => h.Status == "Active")
                .OrderBy(h => h.User.FullName)
                .Select(h => new HostListItemDTO
                {
                    HostId = h.HostId,
                    HostName = h.User.FullName,
                    CompanyName = h.CompanyName,
                    Email = h.User.Email,
                    Status = h.Status
                })
                .ToListAsync();

            return hosts;
        }
    }
}

