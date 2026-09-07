

using COSMETICC.Models;
using COSMETICC.Services;
using COSMETICC.BackgroundServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

namespace Cosmetic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Đặt đoạn này ở đầu file Program.cs hoặc ngay trước khi chạy ứng dụng
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            // MVC
            builder.Services.AddControllersWithViews();

            // ======== AUTOMATION SERVICES ========
            builder.Services.AddSingleton<OrderCodeService>();       // stateful sequence generator
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<OtpService>();
            builder.Services.AddScoped<SmsService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<PdfInvoiceService>();
            builder.Services.AddScoped<FraudDetectionService>();
            builder.Services.AddScoped<SpamFilterService>();
            builder.Services.AddScoped<ComboSuggestionService>();
            builder.Services.AddScoped<ShippingSyncService>();
            builder.Services.AddScoped<RecommendationService>();
            builder.Services.AddScoped<CacheService>();
            builder.Services.AddSignalR();

            // ======== GHN SHIPPING SERVICE ========
            builder.Services.Configure<GhnSettings>(builder.Configuration.GetSection("GHN"));
            builder.Services.AddHttpClient<IGhnService, GhnService>();

            // ======== MOMO PAYMENT SERVICE ========
            builder.Services.Configure<MomoOption>(builder.Configuration.GetSection("Momo"));
            builder.Services.AddScoped<IMomoService, MomoService>();

            // ======== CLOUDINARY SERVICE ========
            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

            // ======== BACKGROUND SERVICES ========
            builder.Services.AddHostedService<AutoCancelOrderService>();
            builder.Services.AddHostedService<ReviewReminderService>();
            builder.Services.AddHostedService<FlashSaleAutoDeactivateService>();
            builder.Services.AddHostedService<AutoRefillReminderService>();
            // IHttpContextAccessor for OtpService
            builder.Services.AddHttpContextAccessor();

            // DB (FIX NAME)
            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            // SESSION
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
            });

            // AUTH
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/signin-google";
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();

            // 🔥 QUAN TRỌNG (THỨ TỰ)
            app.UseAuthentication();
            app.UseAuthorization();

            // Map SignalR Hub
            app.MapHub<COSMETICC.Hubs.NotificationHub>("/notificationHub");

            // ROUTE
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();

        }
    }
}