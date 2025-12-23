using System.Text;
using System.Text.Json.Serialization;
using CondotelManagement.Configurations;
using CondotelManagement.Data;
using CondotelManagement.Hub;
using CondotelManagement.Models;
using CondotelManagement.Services.CloudinaryService;
using CondotelManagement.Services.Interfaces.Cloudinary;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(e => {
    e.EnableDetailedErrors = true; // <--- THÊM DÒNG NÀY
});

// ====================== DB ======================
builder.Services.AddDbContext<CondotelDbVer1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"))
        .LogTo(Console.WriteLine, LogLevel.Information) // Enable SQL logging
        .EnableSensitiveDataLogging()); // Show parameter values

// ====================== Controllers + JSON Fix ======================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Converter cho DateOnly
        options.JsonSerializerOptions.Converters.Add(new CondotelManagement.Helpers.DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new CondotelManagement.Helpers.NullableDateOnlyJsonConverter());
        // Nếu dòng này không có, BE mặc định mong đợi camelCase.
        //options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // 3. THÊM DÒNG NÀY ĐỂ FIX LỖI CRASH (StackOverflow)
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 🚨 BẮT ĐẦU KHỐI FIX LỖI 400 VALIDATION
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    // Tắt hành vi tự động xử lý lỗi validation của ASP.NET Core (khiến lỗi bị generic)
    options.SuppressModelStateInvalidFilter = true;

    // Định nghĩa hàm xử lý lỗi validation tùy chỉnh
    options.InvalidModelStateResponseFactory = context =>
    {
        // Trả về một đối tượng ProblemDetails chứa chi tiết lỗi
        var problemDetails = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(context.ModelState)
        {
            // Tùy chỉnh trạng thái phản hồi
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = "Please check the 'errors' property for details."
        };

        // Quan trọng: Gán lỗi Model State vào thuộc tính 'errors' của ProblemDetails
        // Frontend sẽ đọc thuộc tính này
        problemDetails.Extensions["errors"] = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json", "application/json" }
        };
    };
});
// 🚨 KẾT THÚC KHỐI FIX LỖI 400 VALIDATION

// ============================
// 4️⃣ Swagger + CORS
// ============================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Cloudinary
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// DeepSeek OCR
builder.Services.Configure<DeepSeekOCRSettings>(builder.Configuration.GetSection("DeepSeekOCR"));
builder.Services.AddHttpClient<CondotelManagement.Services.Interfaces.OCR.IDeepSeekOCRService, CondotelManagement.Services.Implementations.OCR.DeepSeekOCRService>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var apiUrl = config["DeepSeekOCR:ApiUrl"] ?? "https://api.deepseek.com/v1/chat/completions";
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// CORS cho React
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",           // Local development
                "https://fiscondotel.com",         // Production frontend
                "https://www.fiscondotel.com"      // Production với www
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();                  // QUAN TRỌNG: cho phép gửi token
    });
});

// Dependency Injection (gồm Auth, Admin, Booking,...)
builder.Services.AddDependencyInjectionConfiguration(builder.Configuration);

// Background Services: Tự động cập nhật booking status, promotion status và voucher status
builder.Services.AddHostedService<CondotelManagement.Services.Background.BookingStatusUpdateService>(); // CONFIRMED → COMPLETED khi qua EndDate
builder.Services.AddHostedService<CondotelManagement.Services.Background.PendingBookingCancellationService>(); // PENDING → CANCELLED sau 10 phút chưa thanh toán
builder.Services.AddHostedService<CondotelManagement.Services.Background.PromotionStatusUpdateService>(); // Active → Inactive khi hết hạn (EndDate < today)
builder.Services.AddHostedService<CondotelManagement.Services.Background.VoucherStatusUpdateService>(); // Active → Expired khi hết hạn (EndDate < today)

// ====================== Build ======================
var app = builder.Build();

// Always enable Swagger (both Dev & Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Condotel API v1");
    c.RoutePrefix = "swagger";  // ⚠ FIX 404 trên VPS
});


app.UseRouting();
app.UseCors("AllowFrontend");
//app.MapHub<ChatHub>("/chatHub", options =>
//{
//    options.Transports = HttpTransportType.WebSockets;
//});
app.MapHub<ChatHub>("/hubs/chat");
app.UseHttpsRedirection();

// Enable static files để serve file từ wwwroot
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
