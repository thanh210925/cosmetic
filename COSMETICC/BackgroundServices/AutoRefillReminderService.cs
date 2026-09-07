using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using COSMETICC.Models;
using COSMETICC.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace COSMETICC.BackgroundServices
{
    public class AutoRefillReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoRefillReminderService> _logger;

        public AutoRefillReminderService(IServiceProvider serviceProvider, ILogger<AutoRefillReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoRefill Reminder Background Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                        var now = DateTime.Now;
                        var dueSubscriptions = await context.AutoRefillSubscriptions
                            .Include(s => s.User)
                            .Include(s => s.Product)
                            .Where(s => s.IsActive && s.NextReminderDate <= now)
                            .ToListAsync(stoppingToken);

                        foreach (var sub in dueSubscriptions)
                        {
                            if (sub.User != null && !string.IsNullOrEmpty(sub.User.Email) && sub.Product != null)
                            {
                                string subject = $"[Cosmetic] Đã đến lúc mua lại {sub.Product.Name}! Tặng bạn Voucher 5%";
                                string body = $@"
                                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                                        <h2 style='color: #800020;'>Chào {sub.User.FullName ?? sub.User.Username},</h2>
                                        <p>Theo lịch đăng ký <strong>Auto-Refill ({sub.IntervalDays} ngày)</strong> của bạn, sản phẩm <strong>{sub.Product.Name}</strong> của bạn sắp hoặc đã sử dụng hết!</p>
                                        <p>Để chăm sóc làn da liên tục không gián đoạn, hãy đặt mua lại ngay hôm nay với ưu đãi độc quyền <strong>Giảm 5%</strong>:</p>
                                        <div style='background-color: #f8f9fa; border: 2px dashed #800020; padding: 15px; text-align: center; margin: 20px 0;'>
                                            <span style='font-size: 20px; font-weight: bold; color: #800020;'>MÃ VOUCHER: AUTO5REFILL</span>
                                        </div>
                                        <p>Cảm ơn bạn đã đồng hành cùng thương hiệu mỹ phẩm của chúng tôi!</p>
                                    </div>";

                                try
                                {
                                    await emailService.SendEmailAsync(sub.User.Email, subject, body);
                                    _logger.LogInformation($"AutoRefill email sent to {sub.User.Email} for product {sub.Product.Name}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Error sending AutoRefill email to {sub.User.Email}");
                                }

                                sub.LastRemindedAt = now;
                                sub.NextReminderDate = now.AddDays(sub.IntervalDays);
                            }
                        }

                        if (dueSubscriptions.Any())
                        {
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in AutoRefill Reminder Service.");
                }

                // Check every 12 hours
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}
