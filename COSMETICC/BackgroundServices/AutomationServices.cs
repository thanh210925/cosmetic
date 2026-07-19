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
    // ────────────────────────────────────────────────────────────────────────
    // AUTO-CANCEL UNPAID ORDERS
    // Runs every 30 minutes. Cancels COD orders not confirmed within 24h.
    // ────────────────────────────────────────────────────────────────────────
    public class AutoCancelOrderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoCancelOrderService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan MaxPendingTime = TimeSpan.FromHours(24);

        public AutoCancelOrderService(IServiceScopeFactory scopeFactory, ILogger<AutoCancelOrderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🤖 AutoCancelOrderService started — checks every {Interval} min", Interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Interval, stoppingToken);

                try
                {
                    await CancelExpiredOrdersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ AutoCancelOrderService error");
                }
            }
        }

        private async Task CancelExpiredOrdersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifSvc = scope.ServiceProvider.GetRequiredService<NotificationService>();

            var cutoff = DateTime.Now - MaxPendingTime;

            // Find orders that are still "Chờ xác nhận" or "Pending" and older than 24h
            var expiredOrders = await db.Orders
                .Include(o => o.Payments)
                .Where(o =>
                    (o.Status == "Chờ xác nhận" || o.Status == "Pending") &&
                    o.OrderDate < cutoff &&
                    o.CancelledAt == null &&
                    !o.Payments.Any(p => p.PaymentStatus == "Paid"))
                .ToListAsync();

            if (!expiredOrders.Any())
            {
                _logger.LogDebug("✅ AutoCancelOrderService: No expired orders found");
                return;
            }

            _logger.LogInformation("🚫 AutoCancelOrderService: Cancelling {Count} expired orders", expiredOrders.Count);

            foreach (var order in expiredOrders)
            {
                order.Status = "Đã hủy (Quá hạn)";
                order.CancelledAt = DateTime.Now;
                db.Orders.Update(order);

                // Send in-app notification to user
                await notifSvc.SendAsync(
                    order.UserId,
                    "Đơn hàng bị hủy tự động",
                    $"Đơn hàng {order.OrderCode ?? $"#{order.Id}"} đã bị hủy do không xác nhận trong 24 giờ.",
                    "order",
                    "/Cart/OrderHistory"
                );

                _logger.LogInformation("✅ Cancelled order {OrderCode} (UserId: {UserId})",
                    order.OrderCode ?? order.Id.ToString(), order.UserId);
            }

            await db.SaveChangesAsync();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // REVIEW REMINDER SERVICE
    // Runs every 6 hours. Sends email to users with delivered orders that haven't reviewed yet.
    // ────────────────────────────────────────────────────────────────────────
    public class ReviewReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReviewReminderService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
        private static readonly int ReminderAfterDays = 3;

        public ReviewReminderService(IServiceScopeFactory scopeFactory, ILogger<ReviewReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("⭐ ReviewReminderService started — checks every {Interval}h", Interval.TotalHours);

            // Wait a bit on startup before first check
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendReviewRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ ReviewReminderService error");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task SendReviewRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailSvc = scope.ServiceProvider.GetRequiredService<EmailService>();
            var notifSvc = scope.ServiceProvider.GetRequiredService<NotificationService>();

            var since = DateTime.Now.AddDays(-ReminderAfterDays);
            var until = DateTime.Now.AddDays(-1); // Don't remind if delivered today

            // Find delivered orders from 1-3 days ago whose users haven't left any review yet
            var eligibleOrders = await db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o =>
                    o.Status == "Đã giao" &&
                    o.OrderDate >= since && o.OrderDate <= until &&
                    o.User != null &&
                    !string.IsNullOrEmpty(o.User.Email) &&
                    // User hasn't reviewed ANY of their purchased products
                    !db.Reviews.Any(r => r.UserId == o.UserId &&
                                        o.OrderDetails.Select(od => od.ProductId).Contains(r.ProductId)))
                .ToListAsync();

            _logger.LogInformation("⭐ ReviewReminderService: Found {Count} orders eligible for review reminder", eligibleOrders.Count);

            foreach (var order in eligibleOrders)
            {
                var productNames = string.Join(", ",
                    order.OrderDetails.Select(od => od.Product?.Name ?? "Sản phẩm").Take(3));

                // Email reminder
                if (!string.IsNullOrEmpty(order.User?.Email))
                {
                    await emailSvc.SendReviewReminderAsync(
                        order.User.Email,
                        order.User.FullName ?? order.User.Username ?? "Bạn",
                        order.OrderCode ?? $"#{order.Id}",
                        productNames
                    );
                }

                // In-app notification
                await notifSvc.SendAsync(
                    order.UserId,
                    "⭐ Bạn cảm thấy thế nào?",
                    $"Hãy đánh giá sản phẩm từ đơn hàng {order.OrderCode ?? $"#{order.Id}"} nhé!",
                    "review",
                    "/Product/List"
                );
            }
        }
    }
}
