using System;
using System.Collections.Generic;
using System.Linq;
using CondotelManagement.Data;
using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Admin;
using CondotelManagement.DTOs.Booking;
using CondotelManagement.DTOs.Payment;
using CondotelManagement.Models;
using CondotelManagement.Repositories;
using CondotelManagement.Services.Interfaces.BookingService;
using CondotelManagement.Services.Interfaces.Payment;
using CondotelManagement.Services.Interfaces.Shared;
using CondotelManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CondotelManagement.Services
{
    public class BookingService : IBookingService
    {
        private readonly CondotelDbVer1Context _context;

    
        private readonly IBookingRepository _bookingRepo;
        private readonly ICondotelRepository _condotelRepo; // để lấy giá phòng
        private readonly IPayOSService _payOSService;
        private readonly IVietQRService _vietQRService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IVoucherService _voucherService;
        private readonly IServicePackageService _servicePackageService;

        public BookingService(
            CondotelDbVer1Context context,
            IBookingRepository bookingRepo,
            ICondotelRepository condotelRepo,
            IPayOSService payOSService,
            IVietQRService vietQRService,
            IConfiguration configuration,
            IEmailService emailService,
            IVoucherService voucherService,
            IServicePackageService servicePackageService)
        {
            _context = context;
            _bookingRepo = bookingRepo;
            _condotelRepo = condotelRepo;
            _payOSService = payOSService;
            _vietQRService = vietQRService;
            _configuration = configuration;
            _emailService = emailService;
            _voucherService = voucherService;
            _servicePackageService = servicePackageService;
        }

        public async Task<IEnumerable<BookingDTO>> GetBookingsByCustomerAsync(int customerId)
        {

            var bookings = await _context.Bookings
     .Include(b => b.Condotel)
         .ThenInclude(c => c.CondotelImages)
     .Where(b => b.CustomerId == customerId)
     .OrderByDescending(b => b.EndDate)
     .ToListAsync();

            // Lấy tất cả RefundRequests để check một lần
            var bookingIds = bookings.Select(b => b.BookingId).ToList();
            var refundRequests = await _context.RefundRequests
                .Where(r => bookingIds.Contains(r.BookingId))
                .ToListAsync();

            var bookingDTOs = bookings.Select(b => 
            {
                var existingRefundRequest = refundRequests.FirstOrDefault(r => r.BookingId == b.BookingId);
                
                // Logic kiểm tra có thể refund không
                bool canRefund = false;
                
                // Kiểm tra booking đã được payout cho host chưa
                if (b.IsPaidToHost != true)
                {
                    // Nếu đã có refund request và đã completed/refunded → không có nút hoàn tiền
                    if (existingRefundRequest != null && 
                        (existingRefundRequest.Status == "Completed" || existingRefundRequest.Status == "Refunded"))
                    {
                        canRefund = false;
                    }
                    // Nếu status là "Cancelled" và chưa có RefundRequest → đây là cancel payment (chưa thanh toán) → KHÔNG có nút hoàn tiền
                    else if (b.Status == "Cancelled" && existingRefundRequest == null)
                    {
                        canRefund = false; // Cancel payment → không có nút hoàn tiền
                    }
                    // Nếu status là "Pending" → chưa thanh toán, không có nút hoàn tiền
                    else if (b.Status == "Pending")
                    {
                        canRefund = false;
                    }
                    // Nếu status là "Confirmed" hoặc "Completed" → có nút hoàn tiền
                    
                    else if (b.Status == "Confirmed")
                    {
                        // Với booking "Confirmed": Phải hoàn tiền trước 2 ngày check-in
                        var now = DateTime.Now;
                        var startDateTime = b.StartDate.ToDateTime(TimeOnly.MinValue);
                        var daysBeforeCheckIn = (startDateTime - now).TotalDays;
                        
                        canRefund = daysBeforeCheckIn >= 2; // Có thể refund nếu còn >= 2 ngày
                    }
                    // Nếu status là "Cancelled" + Rejected RefundRequest → có nút resubmit (no 2-day check)
                    else if (b.Status == "Cancelled" && existingRefundRequest != null && existingRefundRequest.Status == "Rejected")
                    {
                        // ✅ RESUBMIT: Bỏ qua check 2 ngày - cho phép resubmit bất kỳ lúc nào
                        canRefund = true;
                    }
                }

                // Xác định trạng thái refund
                string? refundStatus = null;
                if (b.Status == "Cancelled")
                {
                    if (existingRefundRequest != null)
                    {
                        // Có RefundRequest → lấy status của nó
                        refundStatus = existingRefundRequest.Status; // "Pending", "Refunded", "Completed"
                    }
                    else
                    {
                        // Không có RefundRequest → cancel payment (chưa thanh toán)
                        refundStatus = null; // null = chưa có refund request
                    }
                }
                // Với các status khác (Pending, Confirmed, Completed), refundStatus = null

                return new BookingDTO
                {
                    BookingId = b.BookingId,
                    CondotelId = b.CondotelId,
                    CondotelName = b.Condotel.Name,
                    ThumbnailImage = b.Condotel.CondotelImages.FirstOrDefault()?.ImageUrl,
                    CustomerId = b.CustomerId,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status, // Status đã được update bởi Background Service
                    PromotionId = b.PromotionId,
                    CreatedAt = b.CreatedAt,

                    // Logic hiển thị nút review
                    // Nếu status là "Completed" thì cho phép review (không cần kiểm tra EndDate)
                    CanReview = b.Status == "Completed"
                             && !_context.Reviews.Any(r => r.BookingId == b.BookingId),

                    HasReviewed = _context.Reviews.Any(r => r.BookingId == b.BookingId),
                    
                    // Logic hiển thị nút hoàn tiền
                    CanRefund = canRefund,
                    
                    // Trạng thái hoàn tiền (chỉ có giá trị khi Status = "Cancelled")
                    RefundStatus = refundStatus,
                    
                    // Check-in token và thời gian
                    CheckInToken = b.CheckInToken,
                    CheckInTokenGeneratedAt = b.CheckInTokenGeneratedAt,
                    CheckInTokenUsedAt = b.CheckInTokenUsedAt
                };
            }).ToList();


            return bookingDTOs;
        }

        public async Task<BookingDTO?> GetBookingByIdAsync(int id)
        {
            // Lấy booking với Include Condotel
            var b = await _context.Bookings
                .Include(b => b.Condotel)
                    .ThenInclude(c => c.CondotelImages)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            
            if (b == null) return null;

            // Lấy RefundRequest nếu có
            var refundRequest = await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.BookingId == id);

            // Xác định trạng thái refund
            string? refundStatus = null;
            if (b.Status == "Cancelled")
            {
                if (refundRequest != null)
                {
                    refundStatus = refundRequest.Status; // "Pending", "Refunded", "Completed"
                }
                else
                {
                    refundStatus = null; // null = chưa có refund request (cancel payment)
                }
            }

            var dto = ToDTO(b);
            dto.RefundStatus = refundStatus;
            dto.CondotelName = b.Condotel?.Name ?? "";
            dto.ThumbnailImage = b.Condotel?.CondotelImages.FirstOrDefault()?.ImageUrl;
            dto.CheckInToken = b.CheckInToken;
            dto.CheckInTokenGeneratedAt = b.CheckInTokenGeneratedAt;
            dto.CheckInTokenUsedAt = b.CheckInTokenUsedAt;

            // Set các field khác
            dto.CanReview = b.Status == "Completed"
                         && !_context.Reviews.Any(r => r.BookingId == b.BookingId);
            dto.HasReviewed = _context.Reviews.Any(r => r.BookingId == b.BookingId);

            // Check CanRefund (cần async, nhưng tạm thời dùng logic đơn giản)
            // Có thể gọi CanRefundBookingAsync nếu cần, nhưng cần customerId
            // Tạm thời để false, frontend có thể gọi API can-refund riêng

            return dto;
        }

        [Obsolete("Use GetBookingByIdAsync instead")]
        public BookingDTO GetBookingById(int id)
        {
            var b = _bookingRepo.GetBookingById(id);
            return b == null ? null : ToDTO(b);
        }

        public bool CheckAvailability(int condotelId, DateOnly checkIn, DateOnly checkOut)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var currentTransaction = _context.Database.CurrentTransaction;
            try
            {
                if (currentTransaction == null)
                {
                    using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                    var result = CheckAvailabilityInternal(condotelId, checkIn, checkOut, today);
                    transaction.Commit();
                    return result;
                }
                else
                {
                    return CheckAvailabilityInternal(condotelId, checkIn, checkOut, today);
                }
            }
            catch
            {
                if (currentTransaction == null)
                {
                    currentTransaction?.Rollback();
                }
                throw;
            }
        }

        private bool CheckAvailabilityInternal(
    int condotelId,
    DateOnly checkIn,
    DateOnly checkOut,
    DateOnly today)
        {
            var sql = @"
        SELECT * FROM Booking WITH (UPDLOCK, ROWLOCK)
        WHERE CondotelId = @condotelId 
        AND Status IN ('Confirmed', 'Completed', 'Pending','InStay')
        AND EndDate >= @today
    ";

            var bookings = _context.Bookings
                .FromSqlRaw(sql,
                    new SqlParameter("@condotelId", condotelId),
                    new SqlParameter("@today", today))
                .AsEnumerable()
                .ToList();

            var newCheckIn = checkIn.ToDateTime(new TimeOnly(14, 0));
            var newCheckOut = checkOut.ToDateTime(new TimeOnly(12, 0));

            return !bookings.Any(b =>
            {
                var existingCheckIn = b.StartDate.ToDateTime(new TimeOnly(14, 0));
                var existingCheckOut = b.EndDate.ToDateTime(new TimeOnly(12, 0));

                return newCheckIn < existingCheckOut
                    && newCheckOut > existingCheckIn;
            });
        }



        public async Task<ServiceResultDTO> CreateBookingAsync(CreateBookingDTO dto, int customerId)
        {
            // Validate input
            if (dto == null)
                return ServiceResultDTO.Fail("Dữ liệu đặt phòng không hợp lệ.");

            if (dto.CondotelId <= 0)
                return ServiceResultDTO.Fail("CondotelId không được để trống.");

            if (dto.StartDate == default)
                return ServiceResultDTO.Fail("Ngày bắt đầu không được để trống.");

            if (dto.EndDate == default)
                return ServiceResultDTO.Fail("Ngày kết thúc không được để trống.");

            // Kiểm tra ngày hợp lệ
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (dto.StartDate < today)
                return ServiceResultDTO.Fail("Ngày bắt đầu không được ở trong quá khứ.");

            if (dto.EndDate <= dto.StartDate)
                return ServiceResultDTO.Fail("Ngày kết thúc phải sau ngày bắt đầu.");

            int days = (dto.EndDate.ToDateTime(TimeOnly.MinValue)
                       - dto.StartDate.ToDateTime(TimeOnly.MinValue)).Days;

            if (days > 30)
                return ServiceResultDTO.Fail("Bạn không thể đặt phòng quá 30 ngày.");

            if ((dto.StartDate.ToDateTime(TimeOnly.MinValue)
                - today.ToDateTime(TimeOnly.MinValue)).Days > 365)
            {
                return ServiceResultDTO.Fail("Bạn không thể đặt phòng trước quá 1 năm.");
            }
           

            // Bắt đầu transaction ở đây, không ở trong CheckAvailability
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {

                // Kiểm tra trống
                if (!CheckAvailability(dto.CondotelId, dto.StartDate, dto.EndDate))
                    return ServiceResultDTO.Fail("Condotel không có sẵn trong thời gian này.");

                // Kiểm tra condotel tồn tại và hoạt động
                var condotel = _condotelRepo.GetCondotelById(dto.CondotelId);
                if (condotel == null)
                    return ServiceResultDTO.Fail("Không tìm thấy condotel.");
                if (condotel.Status == "Inactive")
                    return ServiceResultDTO.Fail("Condotel này hiện không hoạt động.");

                // Kiểm tra host không được đặt căn hộ của chính mình
                var hostUserId = await _context.Hosts
                    .Where(h => h.HostId == condotel.HostId)
                    .Select(h => h.UserId)
                    .FirstOrDefaultAsync();

                if (hostUserId == customerId)
                {
                    return ServiceResultDTO.Fail("Chủ căn hộ không thể tự đặt căn hộ của chính mình.");
                }



                // Tính giá cơ bản
                decimal price = condotel.PricePerNight * days;

                int? appliedPromotionId = null;

                if (dto.PromotionId < 0)
                {
                    return ServiceResultDTO.Fail("Khuyến mãi không hợp lệ.");
                }

                // Áp dụng Promotion nếu có
                if (dto.PromotionId.HasValue && dto.PromotionId.Value > 0)
                {
                    var promo = _condotelRepo.GetPromotionById(dto.PromotionId.Value);

                    if (promo != null)
                    {
                        // Validate promotion thuộc về condotel này
                        if (promo.CondotelId != dto.CondotelId)
                        {
                            return ServiceResultDTO.Fail("Khuyến mãi không thuộc về căn hộ này.");
                        }

                        // Validate promotion đang active
                        if (promo.Status != "Active")
                        {
                            return ServiceResultDTO.Fail("Khuyến mãi không còn giá trị.");
                        }

                        // Validate booking dates nằm trong khoảng promotion
                        if (dto.StartDate < promo.StartDate || dto.EndDate > promo.EndDate)
                        {
                            return ServiceResultDTO.Fail($"Ngày đặt phòng phải nằm trong thời gian khuyến mãi({promo.StartDate:yyyy-MM-dd} to {promo.EndDate:yyyy-MM-dd}).");
                        }

                        // Áp dụng discount
                        decimal discountAmount = price * (promo.DiscountPercentage / 100m);
                        price -= discountAmount;
                        appliedPromotionId = promo.PromotionId;
                    }
                    else
                    {
                        return ServiceResultDTO.Fail("Không tìm thấy khuyến mãi này.");
                    }
                }

                // Nếu PromotionId = 0 hoặc null, không áp dụng promotion
                dto.PromotionId = appliedPromotionId;

                // Áp dụng Voucher nếu có (sau khi đã áp dụng promotion)
                int? appliedVoucherId = null;
                if (!string.IsNullOrWhiteSpace(dto.VoucherCode))
                {
                    var voucher = await _voucherService.ValidateVoucherByCodeAsync(
                        dto.VoucherCode.Trim(),
                        dto.CondotelId,
                        customerId,
                        dto.StartDate);

                    if (voucher == null)
                    {
                        return ServiceResultDTO.Fail("Mã phiếu giảm giá không hợp lệ, đã hết hạn hoặc không áp dụng cho lần đặt chỗ này.");
                    }

                    // Áp dụng discount từ voucher
                    if (voucher.DiscountAmount.HasValue && voucher.DiscountAmount.Value > 0)
                    {
                        // Giảm số tiền cố định
                        price -= voucher.DiscountAmount.Value;
                        if (price < 0) price = 0; // Đảm bảo giá không âm
                    }
                    else if (voucher.DiscountPercentage.HasValue && voucher.DiscountPercentage.Value > 0)
                    {
                        // Giảm theo phần trăm
                        decimal discountAmount = price * (voucher.DiscountPercentage.Value / 100m);
                        price -= discountAmount;
                    }

                    appliedVoucherId = voucher.VoucherId;
                }

                // Tính tiền Service Packages nếu có
                decimal servicePackagesTotal = 0;
                List<BookingDetail> bookingDetails = new List<BookingDetail>();


                if (dto.ServicePackages != null && dto.ServicePackages.Any())
                {
                    foreach (var serviceSelection in dto.ServicePackages)
                    {
                        if (serviceSelection.Quantity <= 0) continue;

                        var servicePackage = await _servicePackageService.GetByIdAsync(serviceSelection.ServiceId);
                        if (servicePackage == null || servicePackage.Status != "Active")
                        {
                            return ServiceResultDTO.Fail($"Gói dịch vụ có ID{serviceSelection.ServiceId} không tìm thấy hoặc không hoạt động.");
                        }

                        // Validate service package thuộc về host của condotel này
                        var condotelForValidation = _condotelRepo.GetCondotelById(dto.CondotelId);
                        if (condotelForValidation == null)
                        {
                            return ServiceResultDTO.Fail("Không tìm thấy Condotel này.");
                        }

                        // Lấy service package từ DB để kiểm tra HostID
                        var servicePackageEntity = await _context.ServicePackages.FindAsync(serviceSelection.ServiceId);
                        if (servicePackageEntity == null || servicePackageEntity.HostID != condotelForValidation.HostId)
                        {
                            return ServiceResultDTO.Fail($"Gói dịch vụ không thuộc về chủ sở hữu căn hộ này.");
                        }

                        // Tính tiền: Price * Quantity
                        decimal serviceTotal = servicePackage.Price * serviceSelection.Quantity;
                        servicePackagesTotal += serviceTotal;

                        // Tạo BookingDetail (sẽ lưu sau khi có BookingId)
                        bookingDetails.Add(new BookingDetail
                        {
                            ServiceId = serviceSelection.ServiceId,
                            Quantity = serviceSelection.Quantity,
                            Price = servicePackage.Price
                        });
                    }
                }

                // Tổng tiền = giá phòng (đã áp dụng promotion + voucher) + tiền service packages
                price += servicePackagesTotal;
                var booking = new Booking
                {
                    CondotelId = dto.CondotelId,
                    CustomerId = customerId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    TotalPrice = price,
                    Status = "Pending",
                    PromotionId = appliedPromotionId,
                    VoucherId = appliedVoucherId,
                    CreatedAt = DateTime.Now,
                    GuestFullName = dto.GuestFullName,
                    GuestPhone = dto.GuestPhone,
                    GuestIdNumber = dto.GuestIdNumber
                };
                var responseDto = new BookingDTO
                {
                    BookingId = booking.BookingId,
                    CondotelId = booking.CondotelId,
                    CondotelName = condotel.Name, // Lấy từ condotel đã query trước đó
                    CustomerId = booking.CustomerId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    CheckInAt = booking.StartDate
    .ToDateTime(new TimeOnly(14, 0)),

                    CheckOutAt = booking.EndDate
    .ToDateTime(new TimeOnly(12, 0)),
                    GuestFullName = booking.GuestFullName,
                    GuestPhone = booking.GuestPhone,
                    GuestIdNumber = booking.GuestIdNumber,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status,
                    PromotionId = booking.PromotionId,
                    VoucherId = booking.VoucherId,
                    VoucherCode = dto.VoucherCode,
                    CreatedAt = booking.CreatedAt,
                    CanReview = false,
                    HasReviewed = false,
                    CanRefund = true,
                    RefundStatus = null
                };
                // Set fields
                responseDto.TotalPrice = price;
                responseDto.VoucherId = appliedVoucherId;
                responseDto.Status = "Pending";
                responseDto.CreatedAt = DateTime.Now;

                var entity = ToEntity(responseDto);
                _bookingRepo.AddBooking(entity);
                _bookingRepo.SaveChanges();

                // Lưu BookingDetails sau khi có BookingId
                if (bookingDetails.Any())
                {
                    foreach (var detail in bookingDetails)
                    {
                        detail.BookingId = entity.BookingId;
                        _context.BookingDetails.Add(detail);
                    }
                    await _context.SaveChangesAsync();
                }

                // KHÔNG tăng Voucher UsedCount ở đây
                // Voucher sẽ được tăng UsedCount khi payment thành công (trong webhook)
                // Điều này đảm bảo voucher chỉ bị dùng khi booking thực sự được thanh toán

                responseDto.BookingId = entity.BookingId;
                responseDto.CheckInToken = entity.CheckInToken; // Trả về CheckInToken trong response

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ServiceResultDTO.Ok("Đặt phòng thành công. Vui lòng thanh toán trong 3 phút.", responseDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }




        public BookingDTO UpdateBooking(BookingDTO dto)
        {
            // 1. Validate booking
            var booking = _bookingRepo.GetBookingById(dto.BookingId);
            if (booking == null)
                return null;

            // 2. Không cho sửa CondotelId / CustomerId
            if (dto.CustomerId != booking.CustomerId ||
                dto.CondotelId != booking.CondotelId)
                throw new InvalidOperationException("Không được thay đổi Condotel hoặc khách hàng của đơn đặt phòng.");

            // 3. Validate status hiện tại - KHÔNG cho sửa khi đã Confirmed hoặc Completed
            if (booking.Status == "Confirmed")
                throw new InvalidOperationException("Không thể chỉnh sửa đặt phòng đã được xác nhận thanh toán. Vui lòng hủy đặt phòng và đặt lại.");

            if (booking.Status == "Completed")
                throw new InvalidOperationException("Không thể chỉnh sửa đặt phòng đã hoàn thành.");

            if (booking.Status == "Cancelled")
                throw new InvalidOperationException("Không thể chỉnh sửa đặt phòng đã bị hủy.");

            // 4. Không cho sửa nếu còn dưới 1 ngày tới StartDate
            var today = DateTime.Now.Date;
            var startDate = booking.StartDate; // kiểu DateOnly

            var daysBeforeCheckIn = (startDate.ToDateTime(TimeOnly.MinValue) - today).TotalDays;

            if (daysBeforeCheckIn < 1)
                throw new InvalidOperationException("Phải chỉnh sửa đơn đặt phòng trước ngày bắt đầu ít nhất 1 ngày.");

            // 5. Nếu thay đổi ngày, cần check availability lại
            if (dto.StartDate != booking.StartDate || dto.EndDate != booking.EndDate)
            {
                // Sử dụng transaction để check availability
                using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                try
                {
                    // Check availability (loại trừ booking hiện tại)
                    var todayCheck = DateOnly.FromDateTime(DateTime.UtcNow);
                    var sql = @"
                        SELECT * FROM Booking WITH (UPDLOCK, ROWLOCK)
                        WHERE CondotelId = @condotelId 
                        AND BookingId != @currentBookingId
                        AND Status IN ('Confirmed', 'Completed', 'Pending')
                        AND Status != 'Cancelled'
                        AND EndDate >= @today";

                    var conflictingBookings = _context.Bookings
                        .FromSqlRaw(sql,
                            new SqlParameter("@condotelId", booking.CondotelId),
                            new SqlParameter("@currentBookingId", booking.BookingId),
                            new SqlParameter("@today", todayCheck))
                        .AsEnumerable()
                        .ToList();

                    // Kiểm tra overlap
                    var hasConflict = conflictingBookings.Any(b =>
                        !(dto.EndDate <= b.StartDate || dto.StartDate >= b.EndDate)
                    );

                    if (hasConflict)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Condotel không có sẵn trong khoảng thời gian mới. Vui lòng chọn khoảng thời gian khác.");
                    }

                    transaction.Commit();
                }
                catch
                {
                    throw;
                }
            }

            // 6. Update field hợp lệ
            booking.StartDate = dto.StartDate;
            booking.EndDate = dto.EndDate;
            booking.TotalPrice = dto.TotalPrice;

            // Validate status chuyển đổi hợp lệ - CHỈ cho phép chuyển từ Pending sang Cancelled
            if (dto.Status != booking.Status)
            {
                // Chỉ cho phép chuyển từ Pending sang Cancelled (không cho chuyển sang Confirmed)
                if (booking.Status == "Pending" && dto.Status == "Cancelled")
                {
                    booking.Status = dto.Status;
                }
                else
                {
                    throw new InvalidOperationException($"Không thể chuyển trạng thái từ '{booking.Status}' sang '{dto.Status}'. Chỉ có thể hủy đặt phòng chưa thanh toán.");
                }
            }

            // 7. Lưu DB
            _bookingRepo.UpdateBooking(booking);
            _bookingRepo.SaveChanges();

            return ToDTO(booking);
        }


        public async Task<bool> CancelBooking(int bookingId, int customerId)
        {
            var booking = _bookingRepo.GetBookingById(bookingId);
            if (booking == null || booking.CustomerId != customerId)
                return false;

            // Kiểm tra điều kiện hủy: phải trước ít nhất 2 ngày so với ngày check-in
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var daysUntilCheckIn = (booking.StartDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;
            
            if (daysUntilCheckIn < 2)
            {
                // Không đủ 2 ngày trước check-in
                throw new InvalidOperationException($"Không thể hủy booking. Phải hủy trước ít nhất 2 ngày so với ngày check-in (còn {daysUntilCheckIn} ngày).");
            }
            
            // Nếu booking đã thanh toán (Confirmed/Completed), tự động refund
            if (booking.Status == "Confirmed" || booking.Status == "Completed")
            {
                var refundResult = await RefundBooking(bookingId, customerId);
                if (!refundResult.Success)
                {
                    // Nếu refund thất bại, KHÔNG hủy booking để user có thể thử lại
                    Console.WriteLine($"Hoàn tiền không thành công khi hủy đặt phòng {bookingId}: {refundResult.Message}");
                    return false; // Không hủy booking nếu refund fail
                }
                // Refund request đã được tạo thành công, booking status sẽ được set thành "Cancelled" trong ProcessRefund
                // ProcessRefund đã set status = "Cancelled", không cần set lại ở đây
                return true;
            }
            else if (booking.Status == "Pending")
            {
                // Booking chưa thanh toán, chỉ cần hủy
                booking.Status = "Cancelled";
                _bookingRepo.UpdateBooking(booking);
                return _bookingRepo.SaveChanges();
            }

            return false;
        }
        
        /// <summary>
        /// Hủy thanh toán (cancel payment) - chỉ set status = "Cancelled", KHÔNG refund
        /// Sử dụng khi user hủy thanh toán trước khi thanh toán thành công
        /// </summary>
        public async Task<bool> CancelPayment(int bookingId, int customerId)
        {
            var booking = _bookingRepo.GetBookingById(bookingId);
            if (booking == null || booking.CustomerId != customerId)
                return false;

            // Chỉ cho phép cancel payment nếu booking chưa thanh toán (Pending)
            if (booking.Status == "Pending")
            {
                booking.Status = "Cancelled";
                _bookingRepo.UpdateBooking(booking);
                return _bookingRepo.SaveChanges();
            }
            else if (booking.Status == "Cancelled")
            {
                // Đã bị hủy rồi
                return true;
            }

            // Nếu booking đã thanh toán (Confirmed/Completed), không cho cancel payment
            // Phải dùng CancelBooking để refund
            return false;
        }

        /// <summary>
        /// Kiểm tra xem booking có thể hoàn tiền được không (để hiển thị nút hoàn tiền)
        /// </summary>
        public async Task<bool> CanRefundBooking(int bookingId, int customerId)
        {
            var booking = _bookingRepo.GetBookingById(bookingId);
            if (booking == null || booking.CustomerId != customerId)
                return false;

            // Kiểm tra booking đã được payout cho host chưa
            if (booking.IsPaidToHost == true)
                return false;

            // Kiểm tra xem đã có RefundRequest với status Completed/Refunded chưa
            var existingRefundRequest = await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            // Nếu đã có refund request và đã completed/refunded, thì không có nút hoàn tiền
            if (existingRefundRequest != null && 
                (existingRefundRequest.Status == "Completed" || existingRefundRequest.Status == "Refunded"))
            {
                return false;
            }

            // Nếu status là "Cancelled" và chưa có RefundRequest → đây là cancel payment (chưa thanh toán)
            // KHÔNG có nút hoàn tiền
            if (booking.Status == "Cancelled" && existingRefundRequest == null)
            {
                return false; // Cancel payment → không có nút hoàn tiền
            }

            // Nếu status là "Pending" → chưa thanh toán, không có nút hoàn tiền
            if (booking.Status == "Pending")
            {
                return false;
            }

            // Nếu status là "Confirmed" → có nút hoàn tiền (nếu đủ điều kiện)
            if (booking.Status == "Confirmed")
            {
                var now = DateTime.UtcNow; // Sử dụng UTC để nhất quán với RefundBooking
                
                // Với booking "Confirmed": Phải hủy trước 2 ngày check-in
                var startDateTime = booking.StartDate.ToDateTime(TimeOnly.MinValue);
                var daysBeforeCheckIn = (startDateTime - now).TotalDays;
                
                if (daysBeforeCheckIn < 2)
                    return false; // Quá gần ngày check-in, không cho refund

                return true; // Có thể refund
            }
            
            // Booking "Completed" → KHÔNG cho phép hoàn tiền
            if (booking.Status == "Completed")
            {
                return false; // Booking đã hoàn thành, không thể hoàn tiền
            }

            // Nếu status là "Cancelled" nhưng đã có RefundRequest → có thể đang pending refund
            // Có thể hiển thị nút để xem trạng thái refund
            if (booking.Status == "Cancelled" && existingRefundRequest != null)
            {
                // Đã có refund request, không cần nút hoàn tiền nữa (đã xử lý)
                return false;
            }

            return false;
        }

        public async Task<ServiceResultDTO> RefundBooking(int bookingId, int customerId, string? bankCode = null, string? accountNumber = null, string? accountHolder = null)
        {
            var booking = _bookingRepo.GetBookingById(bookingId);
            if (booking == null || booking.CustomerId != customerId)
                return ServiceResultDTO.Fail("Không tìm thấy booking hoặc bạn không có quyền hủy booking này.");

            // Log bank info từ request
            Console.WriteLine($"[RefundBooking Service] BookingId: {bookingId}, CustomerId: {customerId}");
            Console.WriteLine($"[RefundBooking Service] BankCode: {bankCode}, AccountNumber: {accountNumber}, AccountHolder: {accountHolder}");

            // Kiểm tra booking đã được payout cho host chưa
            if (booking.IsPaidToHost == true)
            {
                return ServiceResultDTO.Fail("Không thể hoàn tiền. Booking này đã được thanh toán cho chủ nhà.");
            }

            // Kiểm tra xem đã có RefundRequest chưa
            var existingRefundRequest = await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            // Nếu đã có refund request và đã completed/refunded, thì không cho tạo mới
            if (existingRefundRequest != null && 
                (existingRefundRequest.Status == "Completed" || existingRefundRequest.Status == "Refunded"))
            {
                return ServiceResultDTO.Fail("Booking này đã được hoàn tiền rồi.");
            }

            // Nếu refund request bị reject, cho phép resubmit 1 lần
            if (existingRefundRequest != null && existingRefundRequest.Status == "Rejected")
            {
                if (existingRefundRequest.ResubmissionCount >= 1)
                {
                    return ServiceResultDTO.Fail("Bạn đã vượt quá số lần gửi lại yêu cầu hoàn tiền (tối đa 1 lần). Vui lòng liên hệ admin.");
                }
                // Cho phép resubmit - sẽ xử lý ở ProcessRefund
            }

            // Nếu status là "Cancelled" → kiểm tra xem có phải cancel payment không
            // Cancel payment: Status = "Cancelled", chưa có RefundRequest, TotalPrice <= 0
            // Cancel booking: Status = "Cancelled", có RefundRequest hoặc TotalPrice > 0, đã thanh toán
            if (booking.Status == "Cancelled")
            {
                if (existingRefundRequest == null && (booking.TotalPrice == null || booking.TotalPrice <= 0))
                {
                    // Cancel payment - chưa thanh toán
                    return ServiceResultDTO.Fail("Không thể hoàn tiền. Booking này đã bị hủy trước khi thanh toán, không có tiền để hoàn lại.");
                }
                // Nếu có existingRefundRequest hoặc TotalPrice > 0 → cho phép resubmit hoặc tạo RefundRequest
            }

            // Nếu status là "Completed" → KHÔNG cho phép hoàn tiền
            if (booking.Status == "Completed")
            {
                return ServiceResultDTO.Fail("Không thể hoàn tiền. Booking đã hoàn thành (đã check-out), không thể hủy và hoàn tiền.");
            }

            // Nếu status là "Refunded" nhưng chưa có refund request, coi như booking vừa hủy và cho phép tạo refund request
            // Hoặc nếu status là "Confirmed" thì cho phép
            if (booking.Status != "Cancelled" && booking.Status != "Confirmed" && booking.Status != "Refunded")
            {
                return ServiceResultDTO.Fail($"Không thể hoàn tiền. Chỉ có thể hoàn tiền cho các booking có trạng thái: Đã xác nhận (Confirmed) hoặc Đã hủy sau khi thanh toán. Trạng thái hiện tại: {booking.Status}.");
            }

            var now = DateTime.UtcNow;
            
            // Logic kiểm tra thời gian refund cho "Confirmed"
            if (booking.Status == "Confirmed")
            {
                // Với booking "Confirmed" (chưa hủy): Phải hủy trước 2 ngày check-in
                var startDateTime = booking.StartDate.ToDateTime(TimeOnly.MinValue);
                var daysBeforeCheckIn = (startDateTime - now).TotalDays;

                if (daysBeforeCheckIn < 2)
                {
                    var daysRemaining = Math.Ceiling(daysBeforeCheckIn);
                    if (daysRemaining < 0)
                    {
                        return ServiceResultDTO.Fail("Không thể hoàn tiền. Ngày check-in đã qua hoặc đang trong thời gian sử dụng phòng.");
                    }
                    else if (daysRemaining == 0)
                    {
                        return ServiceResultDTO.Fail("Không thể hoàn tiền. Bạn chỉ có thể hoàn tiền trước ít nhất 2 ngày so với ngày check-in. Hôm nay là ngày check-in.");
                    }
                    else
                    {
                        return ServiceResultDTO.Fail($"Không thể hoàn tiền. Bạn chỉ có thể hoàn tiền trước ít nhất 2 ngày so với ngày check-in. Còn {daysRemaining} ngày nữa là đến ngày check-in ({booking.StartDate:dd/MM/yyyy}).");
                    }
                }
            }
            else if (booking.Status == "Cancelled" && existingRefundRequest != null && existingRefundRequest.Status == "Rejected")
            {
                // ✅ RESUBMIT REFUND: Bỏ qua check 2 ngày
                // Nếu refund bị reject, được phép resubmit mà không cần check 2 ngày
                // Validation ResubmissionCount sẽ được kiểm tra ở trên
            }

            return await ProcessRefund(booking, "Tenant", null, bankCode, accountNumber, accountHolder);
        }

        public async Task<ServiceResultDTO> AdminRefundBooking(int bookingId, string? reason = null)
        {
            var booking = _bookingRepo.GetBookingById(bookingId);
            if (booking == null)
                return ServiceResultDTO.Fail("Booking not found.");

            // Kiểm tra booking đã được payout cho host chưa
            if (booking.IsPaidToHost == true)
            {
                return ServiceResultDTO.Fail("Không thể hoàn tiền. Booking này đã được thanh toán cho chủ nhà.");
            }

            // Kiểm tra xem đã có RefundRequest với status Completed/Refunded chưa
            var existingRefundRequest = await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            // Nếu đã có refund request và đã completed/refunded, thì không cho tạo mới
            if (existingRefundRequest != null && 
                (existingRefundRequest.Status == "Completed" || existingRefundRequest.Status == "Refunded"))
            {
                return ServiceResultDTO.Fail("Booking này đã được hoàn tiền rồi.");
            }

            if (booking.Status == "Pending")
                return ServiceResultDTO.Fail("Không thể hoàn tiền. Booking này chưa được thanh toán.");

            return await ProcessRefund(booking, "Admin", reason);
        }

        private async Task<ServiceResultDTO> ProcessRefund(Booking booking, string initiatedBy, string? reason = null, string? requestBankCode = null, string? requestAccountNumber = null, string? requestAccountHolder = null)
        {
            // Sử dụng transaction để đảm bảo data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var totalPrice = booking.TotalPrice ?? 0m;
                if (totalPrice <= 0)
                {
                    await transaction.RollbackAsync();
                    return ServiceResultDTO.Fail("Booking does not contain a refundable amount.");
                }

                // Lấy thông tin customer
                var customer = await _context.Users
                    .Include(u => u.Wallets)
                    .FirstOrDefaultAsync(u => u.UserId == booking.CustomerId);

                if (customer == null)
                {
                    await transaction.RollbackAsync();
                    return ServiceResultDTO.Fail("Customer not found.");
                }

                // Ưu tiên sử dụng bank info từ request body, nếu không có thì lấy từ Wallet
                string? bankCode = requestBankCode;
                string? accountNumber = requestAccountNumber;
                string? accountHolder = requestAccountHolder;

                // Log bank info để debug
                Console.WriteLine($"[ProcessRefund] Request bank info - BankCode: {requestBankCode}, AccountNumber: {requestAccountNumber}, AccountHolder: {requestAccountHolder}");
                Console.WriteLine($"[ProcessRefund] Final bank info - BankCode: {bankCode}, AccountNumber: {accountNumber}, AccountHolder: {accountHolder}");

                // Nếu request body không có bank info, lấy từ Wallet
                if (string.IsNullOrEmpty(bankCode) || string.IsNullOrEmpty(accountNumber))
                {
                    Console.WriteLine("[ProcessRefund] Bank info từ request body không đầy đủ, lấy từ Wallet");

                    // Lấy wallet ưu tiên IsDefault và Status = "Active"
                    var customerWallet = customer.Wallets
                        .Where(w => w.Status == "Active")
                        .OrderByDescending(w => w.IsDefault)
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(accountNumber))
                        accountNumber = customerWallet?.AccountNumber;
                    if (string.IsNullOrEmpty(accountHolder))
                        accountHolder = customerWallet?.AccountHolderName;

                    if (string.IsNullOrEmpty(bankCode) && customerWallet != null && !string.IsNullOrEmpty(customerWallet.BankName))
                    {
                        try
                        {
                            var banks = await _vietQRService.GetBanksAsync();
                            var bank = banks.Data.FirstOrDefault(b =>
                                b.Name.Contains(customerWallet.BankName, StringComparison.OrdinalIgnoreCase) ||
                                b.Code.Equals(customerWallet.BankName, StringComparison.OrdinalIgnoreCase) ||
                                b.ShortName.Equals(customerWallet.BankName, StringComparison.OrdinalIgnoreCase));

                            if (bank != null)
                            {
                                bankCode = bank.Code; // Sử dụng mã ngân hàng (MB, VCB, etc.)
                            }
                            else
                            {
                                bankCode = customerWallet.BankName; // Fallback
                            }
                        }
                        catch
                        {
                            bankCode = customerWallet.BankName; // Fallback nếu lỗi
                        }
                    }
                }

                // Kiểm tra xem đã có RefundRequest chưa
                var existingRefundRequest = await _context.RefundRequests
                    .FirstOrDefaultAsync(r => r.BookingId == booking.BookingId);

                RefundRequest refundRequest;
                if (existingRefundRequest != null)
                {
                    // Cập nhật existing request - cập nhật bank info nếu có từ request
                    refundRequest = existingRefundRequest;
                    
                    // Nếu refund bị reject, cho phép resubmit 1 lần
                    if (refundRequest.Status == "Rejected")
                    {
                        // Reset status về Pending để cho phép resubmit
                        refundRequest.Status = "Pending";
                        refundRequest.ResubmissionCount += 1; // Tăng số lần resubmit
                        
                        Console.WriteLine($"[ProcessRefund] RefundRequest {refundRequest.Id} is being resubmitted after rejection. ResubmissionCount: {refundRequest.ResubmissionCount}");
                    }
                    
                    if (!string.IsNullOrEmpty(bankCode))
                        refundRequest.BankCode = bankCode;
                    if (!string.IsNullOrEmpty(accountNumber))
                        refundRequest.AccountNumber = accountNumber;
                    if (!string.IsNullOrEmpty(accountHolder))
                        refundRequest.AccountHolder = accountHolder;
                    refundRequest.UpdatedAt = DateTime.UtcNow;

                    Console.WriteLine($"[ProcessRefund] Updated existing RefundRequest {refundRequest.Id} with bank info - BankCode: {refundRequest.BankCode}, AccountNumber: {refundRequest.AccountNumber}, AccountHolder: {refundRequest.AccountHolder}");
                }
                else
                {
                    // Tạo mới RefundRequest
                    refundRequest = new RefundRequest
                    {
                        BookingId = booking.BookingId,
                        CustomerId = booking.CustomerId,
                        CustomerName = customer.FullName ?? "",
                        CustomerEmail = customer.Email,
                        RefundAmount = totalPrice,
                        Status = "Pending",
                        BankCode = bankCode,
                        AccountNumber = accountNumber,
                        AccountHolder = accountHolder,
                        Reason = reason,
                        CancelDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.RefundRequests.Add(refundRequest);

                    Console.WriteLine($"[ProcessRefund] Created new RefundRequest with bank info - BankCode: {refundRequest.BankCode}, AccountNumber: {refundRequest.AccountNumber}, AccountHolder: {refundRequest.AccountHolder}");
                }

                // Cập nhật booking status
                if (booking.Status != "Cancelled")
                {
                    booking.Status = "Cancelled";
                    _bookingRepo.UpdateBooking(booking);
                }

                // Set PaymentMethod dựa trên initiatedBy
                // - "Tenant": Manual (gửi yêu cầu đến admin, không gọi PayOS)
                // - "Admin": Có thể là Auto hoặc Manual (tùy admin quyết định)
                if (initiatedBy == "Tenant")
                {
                    // Tenant tạo refund request → Chỉ gửi yêu cầu đến admin, KHÔNG gọi PayOS
                    refundRequest.PaymentMethod = "Manual";
                    refundRequest.Status = "Pending";
                    refundRequest.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Console.WriteLine($"[ProcessRefund] RefundRequest {refundRequest.Id} created by Tenant - Manual refund (admin will process)");
                    Console.WriteLine($"[ProcessRefund] Bank info saved - BankCode: {refundRequest.BankCode}, AccountNumber: {refundRequest.AccountNumber}, AccountHolder: {refundRequest.AccountHolder}");

                    return ServiceResultDTO.Ok(
                        "Yêu cầu hoàn tiền đã được gửi đến admin. Admin sẽ xử lý trong thời gian sớm nhất.",
                        new
                        {
                            booking.BookingId,
                            RefundRequestId = refundRequest.Id,
                            RefundAmount = totalPrice,
                            Currency = "VND",
                            InitiatedBy = initiatedBy,
                            Reason = reason,
                            Status = "Pending",
                            PaymentMethod = "Manual",
                            BankInfo = new
                            {
                                BankCode = refundRequest.BankCode,
                                AccountNumber = refundRequest.AccountNumber,
                                AccountHolder = refundRequest.AccountHolder
                            },
                            Note = "Yêu cầu hoàn tiền đã được ghi nhận. Admin sẽ xử lý và chuyển tiền vào tài khoản của bạn."
                        }
                    );
                }

                // Admin tạo refund request → Có thể tạo PayOS link tự động
                // Lưu RefundRequest trước để đảm bảo có ID
                await _context.SaveChangesAsync();
                
                try
                {
                    // Kiểm tra xem đã có PayOS link chưa (nếu đã có TransactionId thì không tạo mới)
                    bool shouldCreatePayOSLink = string.IsNullOrEmpty(refundRequest.TransactionId);

                    PayOSCreatePaymentResponse? refundResponse = null;

                    if (shouldCreatePayOSLink)
                    {
                        // Admin có thể tạo PayOS refund payment link tự động
                        var refundAmount = (int)totalPrice;
                        refundResponse = await _payOSService.CreateRefundPaymentLinkAsync(
                            booking.BookingId,
                            totalPrice,
                            customer.FullName,
                            customer.Email,
                            customer.Phone
                        );
                    }
                    else
                    {
                        Console.WriteLine($"[ProcessRefund] RefundRequest {refundRequest.Id} đã có TransactionId ({refundRequest.TransactionId}), bỏ qua tạo PayOS link mới");
                    }

                    if (refundResponse != null && refundResponse.Code == "00" && refundResponse.Data != null)
                    {
                        // Tạo payment link thành công - KHÔNG set status thành "Refunded" ngay
                        // Chỉ set "Refunded" khi customer thực sự nhận tiền qua PayOS link
                        // Giữ status "Cancelled" hoặc status hiện tại
                        if (booking.Status != "Cancelled")
                        {
                            booking.Status = "Cancelled";
                            _bookingRepo.UpdateBooking(booking);
                        }

                        // Cập nhật RefundRequest - vẫn Pending cho đến khi customer nhận tiền
                        refundRequest.Status = "Pending";
                        refundRequest.PaymentMethod = "Auto";
                        refundRequest.TransactionId = refundResponse.Data.PaymentLinkId?.ToString();
                        refundRequest.UpdatedAt = DateTime.UtcNow;

                        // KHÔNG rollback Voucher UsedCount ở đây
                        // Voucher sẽ được rollback khi refund thực sự thành công (trong Webhook/Return URL)
                        // Điều này đảm bảo voucher chỉ bị rollback khi customer thực sự nhận tiền

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Log sau khi save để verify
                        Console.WriteLine($"[ProcessRefund] After SaveChanges - RefundRequest {refundRequest.Id} bank info saved:");
                        Console.WriteLine($"[ProcessRefund]   BankCode: {refundRequest.BankCode}");
                        Console.WriteLine($"[ProcessRefund]   AccountNumber: {refundRequest.AccountNumber}");
                        Console.WriteLine($"[ProcessRefund]   AccountHolder: {refundRequest.AccountHolder}");

                        var refundInfo = new
                        {
                            booking.BookingId,
                            RefundRequestId = refundRequest.Id,
                            RefundAmount = totalPrice,
                            Currency = "VND",
                            InitiatedBy = initiatedBy,
                            Reason = reason,
                            RefundOrderCode = refundResponse.Data.OrderCode,
                            PaymentLinkId = refundResponse.Data.PaymentLinkId,
                            CheckoutUrl = refundResponse.Data.CheckoutUrl,
                            QrCode = refundResponse.Data.QrCode,
                            Status = "Pending",
                            BankInfo = new
                            {
                                BankCode = refundRequest.BankCode,
                                AccountNumber = refundRequest.AccountNumber,
                                AccountHolder = refundRequest.AccountHolder
                            },
                            Message = "Refund payment link created successfully. Customer can use the link to receive refund. Status will be updated to 'Refunded' when payment is completed."
                        };

                        return ServiceResultDTO.Ok("Refund payment link created successfully.", refundInfo);
                    }
                    else if (refundResponse != null)
                    {
                        // Tạo payment link thất bại - giữ status Pending
                        booking.Status = "Cancelled"; // Giữ status Cancelled nếu chưa refund thành công
                        _bookingRepo.UpdateBooking(booking);

                        // Cập nhật RefundRequest - vẫn Pending
                        refundRequest.Status = "Pending";
                        refundRequest.PaymentMethod = null;
                        refundRequest.UpdatedAt = DateTime.UtcNow;

                        // KHÔNG rollback Voucher UsedCount ở đây
                        // Voucher sẽ được rollback khi refund thực sự thành công (trong Webhook/Return URL)

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Log sau khi save để verify
                        Console.WriteLine($"[ProcessRefund] After SaveChanges (Payment Link Failed) - RefundRequest {refundRequest.Id} bank info saved:");
                        Console.WriteLine($"[ProcessRefund]   BankCode: {refundRequest.BankCode}");
                        Console.WriteLine($"[ProcessRefund]   AccountNumber: {refundRequest.AccountNumber}");
                        Console.WriteLine($"[ProcessRefund]   AccountHolder: {refundRequest.AccountHolder}");

                        return ServiceResultDTO.Ok(
                            $"Refund request created. Failed to create payment link: {refundResponse.Desc}",
                            new
                            {
                                booking.BookingId,
                                RefundRequestId = refundRequest.Id,
                                RefundAmount = totalPrice,
                                Currency = "VND",
                                InitiatedBy = initiatedBy,
                                Reason = reason,
                                Status = "Pending",
                                BankInfo = new
                                {
                                    BankCode = refundRequest.BankCode,
                                    AccountNumber = refundRequest.AccountNumber,
                                    AccountHolder = refundRequest.AccountHolder
                                },
                                Error = refundResponse.Desc,
                                Note = "Please process manual refund"
                            }
                        );
                    }
                    else
                    {
                        // Không tạo PayOS link (đã có TransactionId), chỉ cập nhật bank info
                        booking.Status = "Cancelled";
                        _bookingRepo.UpdateBooking(booking);

                        // Cập nhật RefundRequest - vẫn Pending
                        refundRequest.Status = "Pending";
                        refundRequest.UpdatedAt = DateTime.UtcNow;

                        // KHÔNG rollback Voucher UsedCount ở đây
                        // Voucher sẽ được rollback khi refund thực sự thành công (trong Webhook/Return URL)

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Log sau khi save để verify
                        Console.WriteLine($"[ProcessRefund] After SaveChanges (Skip PayOS - Already has TransactionId) - RefundRequest {refundRequest.Id} bank info saved:");
                        Console.WriteLine($"[ProcessRefund]   BankCode: {refundRequest.BankCode}");
                        Console.WriteLine($"[ProcessRefund]   AccountNumber: {refundRequest.AccountNumber}");
                        Console.WriteLine($"[ProcessRefund]   AccountHolder: {refundRequest.AccountHolder}");

                        return ServiceResultDTO.Ok(
                            "Refund request updated with bank info. PayOS link already exists.",
                            new
                            {
                                booking.BookingId,
                                RefundRequestId = refundRequest.Id,
                                RefundAmount = totalPrice,
                                Currency = "VND",
                                InitiatedBy = initiatedBy,
                                Reason = reason,
                                Status = "Pending",
                                BankInfo = new
                                {
                                    BankCode = refundRequest.BankCode,
                                    AccountNumber = refundRequest.AccountNumber,
                                    AccountHolder = refundRequest.AccountHolder
                                },
                                TransactionId = refundRequest.TransactionId,
                                Note = "Refund request updated. Bank info saved successfully."
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProcessRefund] PayOS Refund Error for Booking {booking.BookingId}: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                    // Kiểm tra xem có phải lỗi "Đơn thanh toán đã tồn tại" không
                    bool isDuplicateOrderError = ex.Message.Contains("Đơn thanh toán đã tồn tại") ||
                                                ex.Message.Contains("Code: 231") ||
                                                ex.Message.Contains("orderCode already exists") ||
                                                ex.Message.Contains("231");

                    // RefundRequest đã được tạo và lưu trước đó, giữ lại
                    // Cập nhật booking status và refund request
                    if (booking.Status != "Cancelled")
                    {
                        booking.Status = "Cancelled";
                        _bookingRepo.UpdateBooking(booking);
                    }

                    refundRequest.Status = "Pending";

                    if (isDuplicateOrderError && string.IsNullOrEmpty(refundRequest.TransactionId))
                    {
                        var refundOrderCode = (long)booking.BookingId * 1000000L + 999999L;
                        refundRequest.TransactionId = $"PayOS-{refundOrderCode}";
                        refundRequest.PaymentMethod = "Auto";
                        Console.WriteLine($"[ProcessRefund] PayOS order already exists (Code: 231), marking as Auto payment method");
                    }
                    else if (!isDuplicateOrderError)
                    {
                        refundRequest.PaymentMethod = "Manual"; // Cần xử lý thủ công
                    }

                    refundRequest.UpdatedAt = DateTime.UtcNow;

                    // KHÔNG rollback Voucher UsedCount ở đây
                    // Voucher sẽ được rollback khi refund thực sự thành công (trong Webhook/Return URL)

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Console.WriteLine($"[ProcessRefund] After SaveChanges (Exception) - RefundRequest {refundRequest.Id} bank info saved:");
                    Console.WriteLine($"[ProcessRefund]   BankCode: {refundRequest.BankCode}");
                    Console.WriteLine($"[ProcessRefund]   AccountNumber: {refundRequest.AccountNumber}");
                    Console.WriteLine($"[ProcessRefund]   AccountHolder: {refundRequest.AccountHolder}");

                    // Trả về success với message rõ ràng
                    var message = isDuplicateOrderError
                        ? "Yêu cầu hoàn tiền đã được tạo thành công. Hệ thống sẽ xử lý hoàn tiền tự động."
                        : "Yêu cầu hoàn tiền đã được tạo thành công. Vui lòng chờ admin xử lý hoàn tiền thủ công.";

                    return ServiceResultDTO.Ok(
                        message,
                        new
                        {
                            booking.BookingId,
                            RefundRequestId = refundRequest.Id,
                            RefundAmount = totalPrice,
                            Currency = "VND",
                            InitiatedBy = initiatedBy,
                            Reason = reason,
                            Status = "Pending",
                            BankInfo = new
                            {
                                BankCode = refundRequest.BankCode,
                                AccountNumber = refundRequest.AccountNumber,
                                AccountHolder = refundRequest.AccountHolder
                            },
                            Note = isDuplicateOrderError
                                ? "Yêu cầu hoàn tiền đã được ghi nhận. Hệ thống sẽ xử lý tự động."
                                : "Yêu cầu hoàn tiền đã được ghi nhận. Admin sẽ xử lý trong thời gian sớm nhất."
                        }
                    );
                }
            }
            catch (Exception outerEx)
            {
                // Nếu có lỗi ở ngoài try block (không phải từ PayOS), rollback transaction
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"[ProcessRefund] Error rolling back transaction: {rollbackEx.Message}");
                }
                return ServiceResultDTO.Fail($"Failed to process refund: {outerEx.Message}");
            }
        }
        // Helper mapping
        private BookingDTO ToDTO(Booking b) => new BookingDTO
        {
            BookingId = b.BookingId,
            CondotelId = b.CondotelId,
            CustomerId = b.CustomerId,
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            TotalPrice = b.TotalPrice,
            Status = b.Status,
            PromotionId = b.PromotionId,
            CreatedAt = b.CreatedAt
        };


        private Booking ToEntity(BookingDTO dto) => new Booking
        {
            BookingId = dto.BookingId,
            CondotelId = dto.CondotelId,
            CustomerId = dto.CustomerId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalPrice = dto.TotalPrice,
            Status = dto.Status,
            PromotionId = dto.PromotionId,
            VoucherId = dto.VoucherId,
            CreatedAt = dto.CreatedAt,
            GuestFullName = dto.GuestFullName,
            GuestPhone = dto.GuestPhone,
            GuestIdNumber = dto.GuestIdNumber
        };

        public IEnumerable<HostBookingDTO> GetBookingsByHost(int hostId)
        {
            return _bookingRepo.GetBookingsByHost(hostId);
        }

        public IEnumerable<HostBookingDTO> GetBookingsByHost(int hostId, DTOs.Booking.BookingFilterDTO? filter)
        {
            return _bookingRepo.GetBookingsByHost(hostId, filter);
        }

        public IEnumerable<HostBookingDTO> GetBookingsByHostAndCustomer(int hostId, int customerId)
        {
            return _bookingRepo.GetBookingsByHostAndCustomer(hostId, customerId);
        }

        public async Task<List<RefundRequestDTO>> GetRefundRequestsAsync(string? searchTerm = null, string? status = "all", DateTime? startDate = null, DateTime? endDate = null, int? condotelTypeId = null)
        {
            // Lấy từ bảng RefundRequests
            var query = _context.RefundRequests
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Condotel)
                        .ThenInclude(c => c.Resort)
                .Include(r => r.Customer)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(r =>
                    r.BookingId.ToString().Contains(search) ||
                    r.CustomerName.ToLower().Contains(search) ||
                    (r.Customer != null && r.Customer.FullName != null && r.Customer.FullName.ToLower().Contains(search)));
            }

            // Filter by status - phân biệt rõ ràng các status
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                if (status == "Pending")
                {
                    query = query.Where(r => r.Status == "Pending");
                }
                else if (status == "Completed")
                {
                    query = query.Where(r => r.Status == "Completed");
                }
                else if (status == "Refunded")
                {
                    query = query.Where(r => r.Status == "Refunded");
                }
                else if (status == "Rejected")
                {
                    query = query.Where(r => r.Status == "Rejected");
                }
            }

            // Filter by condotel type (ResortId) - "loại chỗ ở"
            if (condotelTypeId.HasValue)
            {
                query = query.Where(r => r.Booking != null 
                    && r.Booking.Condotel != null 
                    && r.Booking.Condotel.ResortId == condotelTypeId.Value);
            }

            // Filter by date range (using CancelDate or CreatedAt)
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(r => (r.CancelDate.HasValue && r.CancelDate.Value.Date >= start) ||
                                         (!r.CancelDate.HasValue && r.CreatedAt.Date >= start));
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => (r.CancelDate.HasValue && r.CancelDate.Value <= end) ||
                                         (!r.CancelDate.HasValue && r.CreatedAt <= end));
            }

            var refundRequests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            var result = new List<RefundRequestDTO>();

            foreach (var refundRequest in refundRequests)
            {
                // KHÔNG map status nữa - giữ nguyên status để frontend phân biệt được
                // "Refunded" và "Completed" là 2 status khác nhau
                var displayStatus = refundRequest.Status;

                result.Add(new RefundRequestDTO
                {
                    Id = refundRequest.Id,
                    BookingId = $"BOOK-{refundRequest.BookingId:D3}",
                    CustomerName = refundRequest.CustomerName,
                    RefundAmount = refundRequest.RefundAmount,
                    BankInfo = new BankInfoDTO
                    {
                        BankName = refundRequest.BankCode ?? "",
                        AccountNumber = refundRequest.AccountNumber ?? "",
                        AccountHolder = refundRequest.AccountHolder ?? ""
                    },
                    Status = displayStatus,
                    CancelDate = refundRequest.CancelDate?.ToString("dd/MM/yyyy") ?? refundRequest.CreatedAt.ToString("dd/MM/yyyy")
                });
            }

            return result;
        }

        public async Task<ServiceResultDTO> ConfirmRefundManually(int bookingId)
        {
            Console.WriteLine($"[ConfirmRefundManually] Looking for RefundRequest with BookingId: {bookingId}");
            
            // Debug: Kiểm tra tất cả RefundRequests
            var allRefundRequests = await _context.RefundRequests.ToListAsync();
            Console.WriteLine($"[ConfirmRefundManually] Total RefundRequests in DB: {allRefundRequests.Count}");
            foreach (var rr in allRefundRequests)
            {
                Console.WriteLine($"[ConfirmRefundManually]   - RefundRequest Id: {rr.Id}, BookingId: {rr.BookingId}, Status: {rr.Status}");
            }
            
            // Tìm RefundRequest theo BookingId
            var refundRequest = await _context.RefundRequests
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            Console.WriteLine($"[ConfirmRefundManually] Query result: {(refundRequest != null ? $"Found RefundRequest Id: {refundRequest.Id}" : "Not found")}");

            if (refundRequest == null)
            {
                Console.WriteLine($"[ConfirmRefundManually] RefundRequest not found for BookingId: {bookingId}");
                
                // Kiểm tra xem booking có tồn tại không
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);
                
                if (booking == null)
                {
                    return ServiceResultDTO.Fail("Booking not found.");
                }
                
                // Nếu booking đã cancelled nhưng chưa có RefundRequest, tạo mới
                if (booking.Status == "Cancelled" || booking.Status == "Refunded")
                {
                    Console.WriteLine($"[ConfirmRefundManually] Booking {bookingId} is cancelled but no RefundRequest found. Creating new RefundRequest...");
                    
                    var customer = booking.Customer;
                    if (customer == null)
                    {
                        customer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == booking.CustomerId);
                    }
                    
                    if (customer == null)
                    {
                        return ServiceResultDTO.Fail("Customer not found for this booking.");
                    }
                    
                    // Tạo RefundRequest mới
                    refundRequest = new RefundRequest
                    {
                        BookingId = bookingId,
                        CustomerId = booking.CustomerId,
                        CustomerName = customer.FullName ?? "",
                        CustomerEmail = customer.Email,
                        RefundAmount = booking.TotalPrice ?? 0m,
                        Status = "Pending",
                        CancelDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    _context.RefundRequests.Add(refundRequest);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"[ConfirmRefundManually] Created new RefundRequest {refundRequest.Id} for BookingId: {bookingId}");
                }
                else
                {
                    return ServiceResultDTO.Fail("Refund request not found for this booking. Booking must be cancelled first.");
                }
            }

            Console.WriteLine($"[ConfirmRefundManually] Found RefundRequest {refundRequest.Id} with Status: {refundRequest.Status}");

            if (refundRequest.Status == "Completed" || refundRequest.Status == "Refunded")
                return ServiceResultDTO.Fail("Refund has already been confirmed.");

            if (refundRequest.Status != "Pending")
                return ServiceResultDTO.Fail("Only pending refund requests can be confirmed.");

            // Cập nhật RefundRequest
            refundRequest.Status = "Completed";
            refundRequest.PaymentMethod = "Manual";
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.UpdatedAt = DateTime.UtcNow;
            
            // Rollback Voucher UsedCount khi refund thành công
            if (refundRequest.Booking?.VoucherId.HasValue == true)
            {
                try
                {
                    await _voucherService.RollbackVoucherUsageAsync(refundRequest.Booking.VoucherId.Value);
                    Console.WriteLine($"[ConfirmRefundManually] Đã rollback UsedCount cho Voucher {refundRequest.Booking.VoucherId.Value}");
                }
                catch (Exception voucherEx)
                {
                    Console.WriteLine($"[ConfirmRefundManually] Lỗi khi rollback Voucher UsedCount: {voucherEx.Message}");
                }
            }
            // TODO: Lấy Admin ID từ Claims nếu cần
            // refundRequest.ProcessedBy = adminId;

            // Cập nhật Booking status - KHÔNG đổi thành "Refunded", giữ "Cancelled"
            // Vì booking đã bị hủy, chỉ cần đánh dấu refund đã hoàn thành
            if (refundRequest.Booking != null && refundRequest.Booking.Status != "Cancelled")
            {
                refundRequest.Booking.Status = "Cancelled";
                _bookingRepo.UpdateBooking(refundRequest.Booking);
            }

            await _context.SaveChangesAsync();

            // Gửi email thông báo hoàn tiền thành công cho tenant
            try
            {
                if (!string.IsNullOrEmpty(refundRequest.CustomerEmail))
                {
                    await _emailService.SendRefundConfirmationEmailAsync(
                        refundRequest.CustomerEmail,
                        refundRequest.CustomerName,
                        refundRequest.BookingId,
                        refundRequest.RefundAmount,
                        refundRequest.BankCode,
                        refundRequest.AccountNumber
                    );
                    Console.WriteLine($"[ConfirmRefundManually] Email sent successfully to {refundRequest.CustomerEmail} for booking {refundRequest.BookingId}");
                }
                else
                {
                    Console.WriteLine($"[ConfirmRefundManually] Customer email is empty, skipping email notification for booking {refundRequest.BookingId}");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không fail request nếu email không gửi được
                Console.WriteLine($"[ConfirmRefundManually] Error sending email: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return ServiceResultDTO.Ok(
                "Refund confirmed successfully.",
                new
                {
                    RefundRequestId = refundRequest.Id,
                    BookingId = refundRequest.BookingId,
                    Status = "Completed",
                    RefundAmount = refundRequest.RefundAmount,
                    ProcessedAt = refundRequest.ProcessedAt,
                    BankInfo = new
                    {
                        BankCode = refundRequest.BankCode,
                        AccountNumber = refundRequest.AccountNumber,
                        AccountHolder = refundRequest.AccountHolder
                    }
                }
            );
        }

        public async Task<ServiceResultDTO> RejectRefundRequest(int refundRequestId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return ServiceResultDTO.Fail("Reason is required for rejecting a refund request.");
            }

            // Tìm RefundRequest theo Id (không phải BookingId)
            var refundRequest = await _context.RefundRequests
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Condotel)
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == refundRequestId);

            if (refundRequest == null)
            {
                return ServiceResultDTO.Fail($"Refund request with ID {refundRequestId} not found.");
            }

            // Kiểm tra status hiện tại
            if (refundRequest.Status == "Rejected")
            {
                return ServiceResultDTO.Fail($"Refund request (ID: {refundRequestId}) has already been rejected.");
            }

            if (refundRequest.Status == "Completed" || refundRequest.Status == "Refunded")
            {
                return ServiceResultDTO.Fail($"Cannot reject a refund request (ID: {refundRequestId}) that has already been completed or refunded. Current status: {refundRequest.Status}");
            }

            if (refundRequest.Status != "Pending")
            {
                return ServiceResultDTO.Fail($"Cannot reject a refund request (ID: {refundRequestId}) with status '{refundRequest.Status}'. Only 'Pending' requests can be rejected.");
            }

            // Cập nhật status thành "Rejected"
            refundRequest.Status = "Rejected";
            refundRequest.Reason = reason;
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.UpdatedAt = DateTime.UtcNow;
            // TODO: Lấy Admin ID từ Claims nếu cần
            // refundRequest.ProcessedBy = adminId;

            await _context.SaveChangesAsync();

            // Gửi email thông báo reject cho customer
            try
            {
                if (!string.IsNullOrEmpty(refundRequest.CustomerEmail))
                {
                    // Tạo message thông báo về cơ hội resubmit
                    string resubmissionMessage = refundRequest.ResubmissionCount < 1
                        ? "\n\n⚠️ Thông tin quan trọng: Bạn có thể sửa lại thông tin tài khoản ngân hàng và gửi lại yêu cầu hoàn tiền một lần. Để gửi lại, vui lòng vào chi tiết đơn đặt phòng và chọn 'Gửi lại yêu cầu hoàn tiền'."
                        : ""; // Không có cơ hội resubmit nữa
                    
                    await _emailService.SendRefundRejectionEmailAsync(
                        refundRequest.CustomerEmail,
                        refundRequest.CustomerName,
                        refundRequest.BookingId,
                        refundRequest.RefundAmount,
                        reason + resubmissionMessage
                    );
                    Console.WriteLine($"[RejectRefundRequest] Email sent successfully to {refundRequest.CustomerEmail} for refund request {refundRequestId}. ResubmissionCount: {refundRequest.ResubmissionCount}");
                }
                else
                {
                    Console.WriteLine($"[RejectRefundRequest] Customer email is empty, skipping email notification for refund request {refundRequestId}");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không fail request nếu email không gửi được
                Console.WriteLine($"[RejectRefundRequest] Error sending email: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return ServiceResultDTO.Ok(
                "Refund request rejected successfully.",
                new
                {
                    RefundRequestId = refundRequest.Id,
                    BookingId = refundRequest.BookingId,
                    Status = "Rejected",
                    Reason = reason,
                    ProcessedAt = refundRequest.ProcessedAt
                }
            );
        }

      
    }
}

