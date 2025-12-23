using CondotelManagement.Data;
using CondotelManagement.DTOs;
using CondotelManagement.DTOs.Host;
using CondotelManagement.DTOs.Payment;
using CondotelManagement.Models;
using CondotelManagement.Repositories;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Services.Interfaces.Cloudinary;
using CondotelManagement.Services.Interfaces.OCR;
using CondotelManagement.Services.Interfaces.Payment;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HostModel = CondotelManagement.Models.Host;

// Sử dụng tiền tố đầy đủ để tránh lỗi "ambiguous reference" và loại bỏ 'using CondotelManagement.Models;'

namespace CondotelManagement.Services
{
    public class HostService : IHostService
    {
        private readonly IHostRepository _hostRepo;
        private readonly CondotelDbVer1Context _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IDeepSeekOCRService _ocrService;
        private readonly IVietQRService _vietQRService;

        // SỬA LỖI: Chỉ giữ lại CondotelDbVer1Context vì các repo đã bị xóa
        public HostService(CondotelDbVer1Context context, IHostRepository hostRepo, ICloudinaryService cloudinaryService, IDeepSeekOCRService ocrService, IVietQRService vietQRService)
        {
            _context = context;
            _hostRepo = hostRepo;
            _cloudinaryService = cloudinaryService;
            _ocrService = ocrService;
            _vietQRService = vietQRService;
        }

        public async Task<HostRegistrationResponseDto> RegisterHostAsync(int userId, HostRegisterRequestDto dto)
        {
            // 0. KHỞI TẠO TRANSACTION (Bảo vệ dữ liệu toàn vẹn)
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Tìm User
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new Exception("Không tìm thấy người dùng (UserID từ Token không tồn tại).");
                }

                // 2. Tìm Host và Wallet hiện tại
                var existingHost = await _context.Hosts
                    .Include(h => h.Wallets)
                    .FirstOrDefaultAsync(h => h.UserId == userId);

                // --- Khai báo biến cần thiết ---
                HostModel hostToProcess;
                Wallet walletToProcess = null;
                bool isNewHost = existingHost == null;
                bool isNewWallet = false;

                // --- LOGIC: USER MỚI HOÀN TOÀN (existingHost == null) ---
                if (isNewHost)
                {
                    // 3. Tạo bản ghi Host mới
                    hostToProcess = new HostModel
                    {
                        UserId = userId,
                        PhoneContact = dto.PhoneContact,
                        Address = dto.Address,
                        CompanyName = dto.CompanyName,
                        Status = "Active"
                    };
                    _context.Hosts.Add(hostToProcess);

                    // 4. Lưu Host để lấy HostId
                    await _context.SaveChangesAsync();

                    // 5. Tạo Wallet mới với HostId
                    walletToProcess = new Wallet
                    {
                        HostId = hostToProcess.HostId,
                        BankName = dto.BankName,
                        AccountNumber = dto.AccountNumber,
                        AccountHolderName = dto.AccountHolderName,
                        IsDefault = true,
                        Status = "Active"
                    };
                    _context.Wallets.Add(walletToProcess);
                    isNewWallet = true;

                    // 6. Nâng cấp quyền    
                    user.RoleId = 3;
                }
                // --- LOGIC: USER ĐÃ LÀ HOST (existingHost != null) ---
                else
                {
                    hostToProcess = existingHost;

                    // Cập nhật thông tin Host
                    hostToProcess.PhoneContact = dto.PhoneContact;
                    hostToProcess.Address = dto.Address;
                    hostToProcess.CompanyName = dto.CompanyName;

                    var existingWallet = existingHost.Wallets?.FirstOrDefault(w => w.IsDefault) ?? existingHost.Wallets?.FirstOrDefault();

                    if (existingWallet != null)
                    {
                        // UPDATE Wallet
                        walletToProcess = existingWallet;
                        walletToProcess.BankName = dto.BankName;
                        walletToProcess.AccountNumber = dto.AccountNumber;
                        walletToProcess.AccountHolderName = dto.AccountHolderName;
                    }
                    else
                    {
                        // Tạo Wallet mới cho Host cũ (nếu bị thiếu)
                        walletToProcess = new Wallet
                        {
                            HostId = existingHost.HostId,
                            BankName = dto.BankName,
                            AccountNumber = dto.AccountNumber,
                            AccountHolderName = dto.AccountHolderName,
                            IsDefault = true,
                            Status = "Active"
                        };
                        _context.Wallets.Add(walletToProcess);
                        isNewWallet = true;
                    }
                }

                // 7. LƯU XUỐNG DB
                await _context.SaveChangesAsync();

                // 7. COMMIT TRANSACTION
                await transaction.CommitAsync();

                // 8. TRẢ VỀ
                return new HostRegistrationResponseDto
                {
                    HostId = hostToProcess.HostId,
                    Message = isNewHost ? "Đăng ký làm Host thành công!" : "Cập nhật thông tin thành công!"
                };
            }
            catch (Exception)
            {
                // Có lỗi thì hoàn tác sạch sẽ
                await transaction.RollbackAsync();
                throw;
            }
        }

        public HostModel GetByUserId(int userId)
        {
            return _hostRepo.GetByUserId(userId);
        }

        public Task<bool> CanHostUploadCondotel(int hostId)
        {
            throw new NotImplementedException();
        }

        public async Task<HostProfileDTO> GetHostProfileAsync(int userId)
        {
            var host = await _hostRepo.GetHostProfileAsync(userId);
            if (host == null) return null;

            return new HostProfileDTO
            {
                HostID = host.HostId,
                CompanyName = host.CompanyName,
                Address = host.Address,
                PhoneContact = host.PhoneContact,

                UserID = host.User.UserId,
                FullName = host.User.FullName,
                Email = host.User.Email,
                Phone = host.User.Phone,
                Gender = host.User.Gender,
                DateOfBirth = host.User.DateOfBirth,
                UserAddress = host.User.Address,
                ImageUrl = host.User.ImageUrl,

                Packages = host.HostPackages.Select(hp => new HostPackageDTO
                {
                    PackageID = hp.PackageId,
                    Name = hp.Package.Name,
                    StartDate = hp.StartDate,
                    EndDate = hp.EndDate
                }).ToList(),

                Wallet = host.Wallets?.FirstOrDefault(w => w.IsDefault) != null ? new WalletDTO
                {
                    WalletID = host.Wallets.FirstOrDefault(w => w.IsDefault).WalletId,
                    BankName = host.Wallets.FirstOrDefault(w => w.IsDefault).BankName,
                    AccountNumber = host.Wallets.FirstOrDefault(w => w.IsDefault).AccountNumber,
                    AccountHolderName = host.Wallets.FirstOrDefault(w => w.IsDefault).AccountHolderName
                } : null
            };
        }

        public async Task<bool> UpdateHostProfileAsync(int userId, UpdateHostProfileDTO dto)
        {
            var host = await _hostRepo.GetHostProfileAsync(userId);
            if (host == null) return false;

            // Update HOST
            host.CompanyName = dto.CompanyName;
            host.Address = dto.Address;
            host.PhoneContact = dto.PhoneContact;

            // Update USER
            host.User.FullName = dto.FullName;
            host.User.Phone = dto.Phone;
            host.User.Gender = dto.Gender;
            host.User.DateOfBirth = dto.DateOfBirth;
            host.User.Address = dto.UserAddress;
            host.User.ImageUrl = dto.ImageUrl;

            // Update Wallet (lấy default wallet hoặc wallet đầu tiên)
            var defaultWallet = host.Wallets?.FirstOrDefault(w => w.IsDefault) ?? host.Wallets?.FirstOrDefault();
            if (defaultWallet != null)
            {
                defaultWallet.BankName = dto.BankName;
                defaultWallet.AccountNumber = dto.AccountNumber;
                defaultWallet.AccountHolderName = dto.AccountHolderName;
            }

            await _hostRepo.UpdateHostAsync(host);
            return true;
        }

        public async Task<HostVerificationResponseDTO> VerifyHostWithIdCardAsync(int userId, IFormFile idCardFront, IFormFile idCardBack)
        {
            var response = new HostVerificationResponseDTO { Success = false };

            try
            {
                // 1. Tìm host
                var host = await _context.Hosts.FirstOrDefaultAsync(h => h.UserId == userId);
                if (host == null)
                {
                    response.Message = "Không tìm thấy thông tin host.";
                    return response;
                }

                // 2. Upload ảnh lên Cloudinary
                var frontImageUrl = await _cloudinaryService.UploadImageAsync(idCardFront);
                var backImageUrl = await _cloudinaryService.UploadImageAsync(idCardBack);

                // 3. Lưu URL vào database
                host.IdCardFrontUrl = frontImageUrl;
                host.IdCardBackUrl = backImageUrl;
                host.VerificationStatus = "Pending";
                host.VerifiedAt = null;
                host.VerificationNote = null;

                // 4. Gọi OCR để đọc thông tin
                var frontOCRResult = await _ocrService.ExtractIdCardInfoAsync(frontImageUrl, isFront: true);
                var backOCRResult = await _ocrService.ExtractIdCardInfoAsync(backImageUrl, isFront: false);

                // 5. Map OCR results
                var frontInfo = new IdCardInfoDTO
                {
                    FullName = frontOCRResult.FullName,
                    IdNumber = frontOCRResult.IdNumber,
                    DateOfBirth = frontOCRResult.DateOfBirth,
                    Gender = frontOCRResult.Gender,
                    Nationality = frontOCRResult.Nationality,
                    Address = frontOCRResult.Address
                };

                var backInfo = new IdCardInfoDTO
                {
                    IssueDate = backOCRResult.IssueDate,
                    IssuePlace = backOCRResult.IssuePlace,
                    ExpiryDate = backOCRResult.ExpiryDate
                };

                // 6. Kiểm tra thông tin có hợp lệ không (có thể thêm validation logic ở đây)
                bool isValid = frontOCRResult.Success && backOCRResult.Success;
                
                if (isValid)
                {
                    // So sánh thông tin với thông tin user/host hiện tại (optional)
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Có thể thêm logic so sánh tên, ngày sinh, etc.
                        // Nếu khớp thì set status = "Approved", không thì "Pending" để admin review
                        host.VerificationStatus = "Pending"; // Để admin review
                        host.VerificationNote = "Đang chờ admin xác minh";
                    }
                }
                else
                {
                    host.VerificationStatus = "Pending";
                    host.VerificationNote = "Không thể đọc thông tin từ ảnh. Vui lòng thử lại với ảnh rõ hơn.";
                }

                // 7. Lưu vào database
                await _context.SaveChangesAsync();

                // 8. Trả về response
                response.Success = true;
                response.Message = isValid 
                    ? "Upload ảnh CCCD thành công. Thông tin đã được đọc và đang chờ admin xác minh." 
                    : "Upload ảnh thành công nhưng không thể đọc thông tin. Vui lòng kiểm tra lại chất lượng ảnh.";
                response.VerificationStatus = host.VerificationStatus;
                response.FrontInfo = frontInfo;
                response.BackInfo = backInfo;
            }
            catch (Exception ex)
            {
                response.Message = $"Lỗi khi xác minh: {ex.Message}";
            }

            return response;
        }

        public async Task<ValidateIdCardResponseDTO> ValidateIdCardInfoAsync(int userId)
        {
            var response = new ValidateIdCardResponseDTO { IsValid = false };

            try
            {
                // 1. Tìm host và user
                var host = await _context.Hosts
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(h => h.UserId == userId);

                if (host == null)
                {
                    response.Message = "Không tìm thấy thông tin host.";
                    return response;
                }

                // 2. Kiểm tra xem đã có ảnh CCCD chưa
                if (string.IsNullOrEmpty(host.IdCardFrontUrl))
                {
                    response.Message = "Chưa có ảnh CCCD. Vui lòng upload ảnh CCCD trước.";
                    return response;
                }

                // 3. Đọc lại thông tin từ ảnh CCCD bằng OCR
                var frontOCRResult = await _ocrService.ExtractIdCardInfoAsync(host.IdCardFrontUrl, isFront: true);

                if (!frontOCRResult.Success || string.IsNullOrEmpty(frontOCRResult.FullName) || string.IsNullOrEmpty(frontOCRResult.IdNumber))
                {
                    response.Message = "Không thể đọc thông tin từ ảnh CCCD. Vui lòng upload lại ảnh rõ hơn.";
                    return response;
                }

                // 4. So sánh thông tin với user
                var user = host.User;
                var details = new ValidationDetailsDTO
                {
                    UserFullName = user.FullName,
                    IdCardFullName = frontOCRResult.FullName,
                    IdCardNumber = frontOCRResult.IdNumber,
                    UserDateOfBirth = user.DateOfBirth?.ToString("dd/MM/yyyy"),
                    IdCardDateOfBirth = frontOCRResult.DateOfBirth
                };

                // 5. So sánh tên (loại bỏ dấu và chuyển về chữ hoa để so sánh)
                var normalizedUserName = NormalizeVietnameseName(user.FullName);
                var normalizedIdCardName = NormalizeVietnameseName(frontOCRResult.FullName);
                details.NameMatch = normalizedUserName.Equals(normalizedIdCardName, StringComparison.OrdinalIgnoreCase);

                // 6. So sánh số CCCD (nếu có lưu trong database)
                // Tạm thời chỉ kiểm tra xem có số CCCD hay không
                details.IdNumberMatch = !string.IsNullOrEmpty(frontOCRResult.IdNumber);

                // 7. So sánh ngày sinh (nếu có)
                if (user.DateOfBirth.HasValue && !string.IsNullOrEmpty(frontOCRResult.DateOfBirth))
                {
                    // Parse ngày sinh từ CCCD (format có thể là dd/MM/yyyy hoặc dd-MM-yyyy)
                    if (TryParseDate(frontOCRResult.DateOfBirth, out var idCardDate))
                    {
                        details.DateOfBirthMatch = user.DateOfBirth.Value == DateOnly.FromDateTime(idCardDate);
                    }
                }

                // 8. Gọi VietQR API để xác thực với hệ thống quốc gia
                bool vietQRVerified = false;
                string vietQRMessage = string.Empty;
                try
                {
                    // Chuẩn hóa tên để gửi lên VietQR (chuyển về chữ hoa, loại bỏ dấu)
                    var normalizedNameForVietQR = NormalizeVietnameseName(frontOCRResult.FullName).ToUpper();
                    var vietQRResult = await _vietQRService.VerifyCitizenAsync(frontOCRResult.IdNumber, normalizedNameForVietQR);
                    
                    // Code "00" nghĩa là thành công - số CCCD và tên khớp với hệ thống quốc gia
                    vietQRVerified = vietQRResult.Code == "00";
                    vietQRMessage = vietQRResult.Desc;
                    details.VietQRVerified = vietQRVerified;
                    details.VietQRMessage = vietQRMessage;
                }
                catch (Exception ex)
                {
                    // Nếu VietQR API lỗi, vẫn tiếp tục với validation nội bộ
                    vietQRMessage = $"Không thể xác thực qua VietQR: {ex.Message}";
                    details.VietQRVerified = false;
                    details.VietQRMessage = vietQRMessage;
                }

                // 9. Kết luận - Kết hợp validation nội bộ và VietQR
                // Nếu VietQR verify thành công, coi như valid
                // Nếu không, vẫn kiểm tra validation nội bộ
                bool internalValidation = details.NameMatch && details.IdNumberMatch;
                response.IsValid = vietQRVerified || internalValidation;
                response.Details = details;

                if (response.IsValid)
                {
                    if (vietQRVerified)
                    {
                        response.Message = "Thông tin CCCD đã được xác thực thành công qua hệ thống quốc gia.";
                    }
                    else
                    {
                        response.Message = "Thông tin CCCD khớp với thông tin tài khoản.";
                    }
                    
                    // Cập nhật status nếu chưa được approve
                    if (host.VerificationStatus != "Approved")
                    {
                        host.VerificationStatus = "Approved";
                        host.VerifiedAt = DateTime.UtcNow;
                        host.VerificationNote = vietQRVerified 
                            ? "Thông tin CCCD đã được xác thực qua VietQR API" 
                            : "Thông tin CCCD đã được xác thực tự động";
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    var mismatches = new List<string>();
                    if (!details.NameMatch) mismatches.Add("Tên không khớp");
                    if (!details.IdNumberMatch) mismatches.Add("Số CCCD không hợp lệ");
                    if (!details.DateOfBirthMatch && user.DateOfBirth.HasValue) mismatches.Add("Ngày sinh không khớp");
                    
                    if (!string.IsNullOrEmpty(vietQRMessage) && !vietQRVerified)
                    {
                        mismatches.Add($"VietQR: {vietQRMessage}");
                    }

                    response.Message = $"Thông tin CCCD không khớp: {string.Join(", ", mismatches)}. Vui lòng kiểm tra lại.";
                }
            }
            catch (Exception ex)
            {
                response.Message = $"Lỗi khi xác thực: {ex.Message}";
            }

            return response;
        }

        // Helper method để chuẩn hóa tên tiếng Việt (loại bỏ dấu)
        private string NormalizeVietnameseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;

            // Loại bỏ khoảng trắng thừa và chuyển về chữ hoa
            name = name.Trim().ToUpper();
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ");

            // Loại bỏ dấu tiếng Việt
            var normalizedString = name.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        public async Task<List<TopHostDTO>> GetTopHostsByRatingAsync(int topCount = 10)
        {
            try
            {
                // Lấy tất cả hosts active với thông tin user
                var hosts = await _context.Hosts
                    .Include(h => h.User)
                    .Where(h => h.Status == "Active")
                    .AsNoTracking()
                    .ToListAsync();

                if (!hosts.Any())
                {
                    return new List<TopHostDTO>();
                }

                // Lấy tất cả condotels của các hosts này
                var hostIds = hosts.Select(h => h.HostId).ToList();
                var condotels = await _context.Condotels
                    // Chỉ tính các condotel còn Active (tránh tính rating cho condotel đã bị Inactive/ẩn)
                    .Where(c => hostIds.Contains(c.HostId) && c.Status == "Active")
                    .Select(c => new { c.HostId, c.CondotelId })
                    .AsNoTracking()
                    .ToListAsync();

                if (!condotels.Any())
                {
                    return new List<TopHostDTO>();
                }

                // Lấy tất cả reviews Visible của các condotels này
                var condotelIds = condotels.Select(c => c.CondotelId).ToList();
                var reviews = await _context.Reviews
                    // Thực tế trong hệ thống: review "bị xóa" dùng Status = "Deleted"
                    // Nên top host sẽ tính tất cả review không bị xóa (bao gồm cả NULL/Visible/Active nếu DB có dữ liệu cũ)
                    .Where(r => condotelIds.Contains(r.CondotelId) && r.Status != "Deleted")
                    .Select(r => new { r.CondotelId, r.Rating })
                    .AsNoTracking()
                    .ToListAsync();

                // Group reviews theo HostId thông qua CondotelId
                var hostReviewStats = condotels
                    .GroupJoin(reviews,
                        condotel => condotel.CondotelId,
                        review => review.CondotelId,
                        (condotel, reviewGroup) => new
                        {
                            condotel.HostId,
                            Reviews = reviewGroup.ToList()
                        })
                    .GroupBy(x => x.HostId)
                    .Select(g => new
                    {
                        HostId = g.Key,
                        AllReviews = g.SelectMany(x => x.Reviews).ToList()
                    })
                    .ToList();

                var hostDTOs = new List<TopHostDTO>();

                foreach (var host in hosts)
                {
                    var stats = hostReviewStats.FirstOrDefault(s => s.HostId == host.HostId);
                    var totalCondotels = condotels.Count(c => c.HostId == host.HostId);

                    // Nếu host chưa có condotel active thì không đưa vào "top"
                    if (totalCondotels <= 0) continue;

                    var totalReviews = stats?.AllReviews.Count ?? 0;
                    var averageRating = totalReviews > 0
                        ? stats!.AllReviews.Average(r => (double)r.Rating)
                        : 0d;

                    hostDTOs.Add(new TopHostDTO
                    {
                        HostId = host.HostId,
                        CompanyName = host.CompanyName ?? string.Empty,
                        FullName = host.User?.FullName,
                        AvatarUrl = host.User?.ImageUrl,
                        AverageRating = Math.Round(averageRating, 2),
                        TotalReviews = totalReviews,
                        TotalCondotels = totalCondotels,
                        Rank = 0 // Sẽ set sau khi sort
                    });
                }

                // Sắp xếp theo AverageRating giảm dần, sau đó theo TotalReviews giảm dần
                // Nếu chưa có review (AverageRating=0, TotalReviews=0) thì ưu tiên host có nhiều condotel hơn
                var sortedHosts = hostDTOs
                    .OrderByDescending(h => h.AverageRating)
                    .ThenByDescending(h => h.TotalReviews)
                    .ThenByDescending(h => h.TotalCondotels)
                    .Take(topCount)
                    .ToList();

                // Set rank
                for (int i = 0; i < sortedHosts.Count; i++)
                {
                    sortedHosts[i].Rank = i + 1;
                }

                return sortedHosts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetTopHostsByRating] Error: {ex.Message}");
                Console.WriteLine($"[GetTopHostsByRating] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // Helper method để parse ngày từ string
        private bool TryParseDate(string dateString, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrEmpty(dateString)) return false;

            // Thử các format phổ biến
            var formats = new[] { "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            // Thử parse tự động
            return DateTime.TryParse(dateString, out date);
        }
    }
}