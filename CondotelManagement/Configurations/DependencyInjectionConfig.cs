// --- CÁC USING CŨ CỦA BẠN ---
using CondotelManagement.Repositories;
using CondotelManagement.Repositories.Implementations;
using CondotelManagement.Repositories.Implementations.Admin;
using CondotelManagement.Repositories.Implementations.Auth;
using CondotelManagement.Repositories.Interfaces;
using CondotelManagement.Repositories.Interfaces.Admin;
using CondotelManagement.Repositories.Interfaces.Auth;
using CondotelManagement.Services;
using CondotelManagement.Services.Implementations;
using CondotelManagement.Services.Implementations.Admin;
using CondotelManagement.Services.Implementations.Auth;
using CondotelManagement.Services.Implementations.Blog;
using CondotelManagement.Services.Implementations.Shared;
using CondotelManagement.Services.Implementations.Tenant;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Services.Interfaces.Admin;
using CondotelManagement.Services.Interfaces.Auth;
using CondotelManagement.Services.Interfaces.Blog;
using CondotelManagement.Services.Interfaces.BookingService;
using CondotelManagement.Services.Interfaces.Shared;
using CondotelManagement.Services.Interfaces.Tenant;
using CondotelManagement.Services.Interfaces.Payment;
using CondotelManagement.Services.Implementations.Payment;
using CondotelManagement.Repositories.Interfaces.Amenity;
using CondotelManagement.Repositories.Implementations.Amenity;
using CondotelManagement.Services.Interfaces.Amenity;
using CondotelManagement.Services.Implementations.Amenity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CondotelManagement.Repositories.Implementations.Chat;
using CondotelManagement.Repositories.Interfaces.Chat;
using CondotelManagement.Services.Implementations.Chat;
using CondotelManagement.Services.Interfaces.Chat;
using CondotelManagement.Services.Interfaces.Host;
using CondotelManagement.Services.Implementations.Host;
using CondotelManagement.Services.Interfaces.Wallet;
using CondotelManagement.Services.Implementations.Wallet;
using CondotelManagement.Services.Background;
using CondotelManagement.Services.BackgroundServices;

namespace CondotelManagement.Configurations
{
    public static class DependencyInjectionConfig
    {
        public static void AddDependencyInjectionConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            // --- Đăng ký Dependency Injection (DI) ---
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // --- Admin ---
            services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();

            // --- Admin User Management ---
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserService, UserService>();

            // --- Auth ---
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IAuthService, AuthService>();

            // --- Shared ---
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IProfileService, ProfileService>();

            // --- Condotel ---
            services.AddScoped<ICondotelRepository, CondotelRepository>();
            services.AddScoped<ICondotelService, CondotelService>();

            // --- Location ---
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<ILocationService, LocationService>();

            // --- Resort ---
            services.AddScoped<IResortRepository, ResortRepository>();
            services.AddScoped<IResortService, ResortService>();

            // --- Promotion ---
            services.AddScoped<IPromotionRepository, PromotionRepository>();
            services.AddScoped<IPromotionService, PromotionService>();

            // --- Host ---
            services.AddScoped<IHostRepository, HostRepository>();
            services.AddScoped<IHostService, HostService>();
            services.AddScoped<IHostPayoutService, HostPayoutService>();

            // --- Wallet ---
            services.AddScoped<IWalletService, WalletService>();

            // --- Booking ---
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IBookingService, BookingService>();

            // --- Tenant Review ---
            services.AddScoped<ITenantReviewService, TenantReviewService>();

            // --- Customer ---
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICustomerService, CustomerService>();

            // --- Service Package ---
            services.AddScoped<IServicePackageRepository, ServicePackageRepository>();
            services.AddScoped<IServicePackageService, ServicePackageService>();

            // --- Host Report ---
            services.AddScoped<IHostReportRepository, HostReportRepository>();
            services.AddScoped<IHostReportService, HostReportService>();

            // --- Admin Report ---
            services.AddScoped<IAdminReportRepository, AdminReportRepository>();
            services.AddScoped<IAdminReportService, AdminReportService>();

			// --- Voucher ---
			services.AddScoped<IVoucherRepository, VoucherRepository>();
			services.AddScoped<IVoucherService, VoucherService>();
            // --- Blog (THÊM MỚI) ---
            services.AddScoped<IBlogService, BlogService>();

			// --- Review ---
			services.AddScoped<IReviewRepository, ReviewRepository>();
			services.AddScoped<IReviewService, ReviewService>();

            //--Chat--
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddSignalR();
            // --- Background Services ---
            services.AddHostedService<BookingStatusUpdateService>();
            services.AddHostedService<BookingTimeoutBackgroundService>();

            // --- Payment (PayOS) ---
            services.AddHttpClient<IPayOSService, PayOSService>((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = config["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";
                var clientId = config["PayOS:ClientId"];
                var apiKey = config["PayOS:ApiKey"];
                
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                
                // Thêm headers
                if (!string.IsNullOrEmpty(clientId))
                    client.DefaultRequestHeaders.Add("x-client-id", clientId);
                if (!string.IsNullOrEmpty(apiKey))
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", "CondotelManagement/1.0");
            });

            // --- Payment (VietQR) ---
            services.AddHttpClient<IVietQRService, VietQRService>((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = config["VietQR:BaseUrl"] ?? "https://api.vietqr.io";
                var clientId = config["VietQR:ClientId"];
                var apiKey = config["VietQR:ApiKey"];
                
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                
                // Thêm headers
                if (!string.IsNullOrEmpty(clientId))
                    client.DefaultRequestHeaders.Add("x-client-id", clientId);
                if (!string.IsNullOrEmpty(apiKey))
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", "CondotelManagement/1.0");
            });

            // --- 2. THEM CAC DONG MOI O DAY ---
            // Dang ky Service cho Package
            services.AddScoped<IPackageService, PackageService>();

            // SỬA LẠI: Từ Singleton thành Scoped vì PackageFeatureService dùng DbContext
            services.AddScoped<IPackageFeatureService, PackageFeatureService>();

            // --- Utility ---
            services.AddScoped<IUtilitiesRepository, UtilitiesRepository>();
			services.AddScoped<IUtilitiesService, UtilitiesService>();

			// --- Amenity ---
			services.AddScoped<IAmenityRepository, AmenityRepository>();
			services.AddScoped<IAmenityService, AmenityService>();

            // --- Cấu hình JWT Authentication ---
            // --- Cấu hình JWT Authentication ---
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),

        // QUAN TRỌNG: Để mặc định hoặc map đúng với token của bạn
        // NameClaimType = ClaimTypes.NameIdentifier, // Hoặc để mặc định
        // RoleClaimType = ClaimTypes.Role,

        ClockSkew = TimeSpan.Zero // Không cần skew nếu frontend/backend cùng timezone
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },

        // THÊM để debug authentication failures
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for user: {context.Principal.Identity.Name}");
            return Task.CompletedTask;
        }
    };
});
        }
    }
}
