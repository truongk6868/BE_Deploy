using System.Security.Claims;
using CondotelManagement.Data;
using CondotelManagement.Models;
using CondotelManagement.Repositories.Interfaces.Auth;
using CondotelManagement.Services.Interfaces.Cloudinary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondotelManagement.Controllers.Upload
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly ICloudinaryService _cloud;
        private readonly IAuthRepository _repo;
        private readonly CondotelDbVer1Context _context;


        public UploadController(ICloudinaryService cloud, IAuthRepository repo, CondotelDbVer1Context context)
        {
            _cloud = cloud;
            _repo = repo;
            _context = context;
        }

        public class CreateCondotelDetailRequest
        {
            public string? BuildingName { get; set; }
            public string? RoomNumber { get; set; }
            public byte Beds { get; set; }
            public byte Bathrooms { get; set; }
            public string? SafetyFeatures { get; set; }
            public string? HygieneStandards { get; set; }
            public string? Status { get; set; }
        }

        // Upload ảnh chung
        [HttpPost("image")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null) return BadRequest("No file uploaded");
            var url = await _cloud.UploadImageAsync(file);
            return Ok(new { imageUrl = url });
        }

        // ✅ Upload ảnh cho user hiện tại
        [HttpPost("user-image")]
        [Authorize] // ✅ Cho phép tất cả user đã đăng nhập
        public async Task<IActionResult> UploadUserImage(IFormFile file)
        {
            if (file == null)
                return BadRequest(new { message = "No file uploaded" });

            // Lấy email từ JWT token
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Invalid token" });

            // Upload ảnh lên Cloudinary
            var imageUrl = await _cloud.UploadImageAsync(file);

            // Cập nhật user trong DB
            var user = await _repo.GetByEmailAsync(email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.ImageUrl = imageUrl;
            await _repo.UpdateUserAsync(user);

            return Ok(new
            {
                message = "Profile image updated successfully",
                imageUrl
            });
        }

        // ✅ Upload ảnh cho Condotel và lưu vào bảng CondotelImage
        [HttpPost("condotel/{condotelId:int}/image")]
        [Authorize]
        public async Task<IActionResult> UploadCondotelImage([FromRoute] int condotelId, IFormFile file, [FromForm] string? caption)
        {
            if (file == null) return BadRequest(new { message = "No file uploaded" });

            var exists = await Task.Run(() => _context.Condotels.Any(c => c.CondotelId == condotelId));
            if (!exists) return NotFound(new { message = "Không tìm thấy condotel" });

            var imageUrl = await _cloud.UploadImageAsync(file);

            var image = new CondotelImage
            {
                CondotelId = condotelId,
                ImageUrl = imageUrl,
                Caption = caption
            };

            _context.CondotelImages.Add(image);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Condotel image uploaded successfully",
                imageId = image.ImageId,
                imageUrl = image.ImageUrl,
                caption = image.Caption
            });
        }

        // ✅ Tạo bản ghi Detail cho Condotel
        [HttpPost("condotel/{condotelId:int}/detail")]
        [Authorize]
        public async Task<IActionResult> CreateCondotelDetail([FromRoute] int condotelId, [FromBody] CreateCondotelDetailRequest request)
        {
            var exists = await Task.Run(() => _context.Condotels.Any(c => c.CondotelId == condotelId));
            if (!exists) return NotFound(new { message = "Không tìm thấy condotel" });

            var detail = new CondotelDetail
            {
                CondotelId = condotelId,
                BuildingName = request.BuildingName,
                RoomNumber = request.RoomNumber,
                SafetyFeatures = request.SafetyFeatures,
                HygieneStandards = request.HygieneStandards,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status
            };

            _context.CondotelDetails.Add(detail);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Condotel detail created successfully",
                detailId = detail.DetailId
            });
        }


    }
}
