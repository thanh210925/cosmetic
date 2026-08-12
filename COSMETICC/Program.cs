

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
            
            // ======== GHN SHIPPING SERVICE ========
            builder.Services.Configure<GhnSettings>(builder.Configuration.GetSection("GHN"));
            builder.Services.AddHttpClient<IGhnService, GhnService>();

            // ======== MOMO PAYMENT SERVICE ========
            builder.Services.Configure<MomoOption>(builder.Configuration.GetSection("Momo"));
            builder.Services.AddScoped<IMomoService, MomoService>();

            // ======== BACKGROUND SERVICES ========
            builder.Services.AddHostedService<AutoCancelOrderService>();
            builder.Services.AddHostedService<ReviewReminderService>();
            builder.Services.AddHostedService<FlashSaleAutoDeactivateService>();
            // IHttpContextAccessor for OtpService
            builder.Services.AddHttpContextAccessor();

            // DB (FIX NAME)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // SESSION
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
            });

            // AUTH
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

            // 🔥 QUAN TRỌNG (THỨ TỰ)
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            // ROUTE
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}