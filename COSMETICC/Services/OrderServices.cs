using System;
using System.Linq;
using System.Threading.Tasks;
using COSMETICC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace COSMETICC.Services
{
    // ────────────────────────────────────────────────────────────────────────
    // ORDER CODE GENERATOR
    // ────────────────────────────────────────────────────────────────────────
    public class OrderCodeService
    {
        private static readonly object _lock = new();
        private static int _sequence = 0;

        /// <summary>Generates unique order code: ML-YYYYMMDD-XXXX</summary>
        public string Generate()
        {
            lock (_lock)
            {
                _sequence = (_sequence % 9999) + 1;
                return $"ML-{DateTime.Now:yyyyMMdd}-{_sequence:D4}";
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // SPAM FILTER SERVICE
    // ────────────────────────────────────────────────────────────────────────
    public class SpamFilterService
    {
        private readonly ILogger<SpamFilterService> _logger;

        private static readonly string[] SpamKeywords = {
            "casino", "cờ bạc", "cá độ", "bet", "xổ số", "trúng thưởng",
            "http://", "https://", "click here", "free money",
            "spam", "quảng cáo", "bán hàng", "liên hệ ngay", "mua ngay"
        };

        private static readonly string[] ToxicKeywords = {
            "chửi", "địt", "đm", "cmm", "fuck", "shit", "trash", "lừa đảo",
            "scam", "giả mạo", "hàng fake", "hàng giả"
        };

        public SpamFilterService(ILogger<SpamFilterService> logger)
        {
            _logger = logger;
        }

        public (bool IsSpam, bool IsToxic, string Reason) Analyze(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, false, "");

            var lower = text.ToLower();

            foreach (var kw in SpamKeywords)
                if (lower.Contains(kw))
                {
                    _logger.LogWarning("🚫 Spam detected: keyword '{Kw}' in review", kw);
                    return (true, false, $"Chứa từ khóa spam: '{kw}'");
                }

            foreach (var kw in ToxicKeywords)
                if (lower.Contains(kw))
                {
                    _logger.LogWarning("🚫 Toxic content: keyword '{Kw}' in review", kw);
                    return (false, true, $"Chứa nội dung không phù hợp: '{kw}'");
                }

            // Length heuristic
            if (text.Length < 5)
                return (true, false, "Bình luận quá ngắn");

            if (CountUrls(lower) > 1)
                return (true, false, "Chứa nhiều đường link");

            return (false, false, "");
        }

        private static int CountUrls(string text)
        {
            int count = 0, idx = 0;
            while ((idx = text.IndexOf("http", idx)) >= 0) { count++; idx += 4; }
            return count;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // FRAUD DETECTION SERVICE
    // ────────────────────────────────────────────────────────────────────────
    public class FraudDetectionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FraudDetectionService> _logger;

        // Thresholds
        private const decimal HighValueThreshold = 10_000_000m; // 10 triệu VND
        private const int MaxOrdersPerHour = 5;

        public FraudDetectionService(AppDbContext context, ILogger<FraudDetectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool IsSuspect, string Reason)> CheckOrderAsync(int userId, decimal orderTotal)
        {
            // Rule 1: Giá trị đơn cực cao
            if (orderTotal >= HighValueThreshold)
            {
                var reason = $"Giá trị đơn hàng rất cao: {orderTotal:N0}₫";
                _logger.LogWarning("⚠️ Fraud Rule 1 triggered for User {UserId}: {Reason}", userId, reason);
                return (true, reason);
            }

            // Rule 2: Quá nhiều đơn trong 1 giờ
            var oneHourAgo = DateTime.Now.AddHours(-1);
            var recentOrders = await _context.Orders
                .CountAsync(o => o.UserId == userId && o.OrderDate >= oneHourAgo);

            if (recentOrders >= MaxOrdersPerHour)
            {
                var reason = $"Đặt {recentOrders} đơn hàng trong vòng 1 giờ";
                _logger.LogWarning("⚠️ Fraud Rule 2 triggered for User {UserId}: {Reason}", userId, reason);
                return (true, reason);
            }

            // Rule 3: Tài khoản mới, đơn lớn
            var user = await _context.Users.FindAsync(userId);
            if (user?.CreatedAt != null && (DateTime.Now - user.CreatedAt.Value).TotalDays < 1 && orderTotal > 2_000_000m)
            {
                var reason = $"Tài khoản mới (<24h) đặt đơn {orderTotal:N0}₫";
                _logger.LogWarning("⚠️ Fraud Rule 3 triggered for User {UserId}: {Reason}", userId, reason);
                return (true, reason);
            }

            return (false, "");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // NOTIFICATION SERVICE (In-app)
    // ────────────────────────────────────────────────────────────────────────
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendAsync(int? userId, string title, string message, string type = "system", string? link = null)
        {
            var notif = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAllReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // COMBO SUGGESTION SERVICE
    // ────────────────────────────────────────────────────────────────────────
    public class ComboSuggestionService
    {
        private readonly AppDbContext _context;

        public ComboSuggestionService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Suggests complementary products that are often bought together.
        /// Strategy: Find products from categories that complement the given product's category.
        /// </summary>
        public async Task<System.Collections.Generic.List<Product>> GetComboAsync(int productId, int count = 3)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return new();

            // Find users who bought this product and what else they bought
            var cobuyers = await _context.OrderDetails
                .Where(od => od.ProductId == productId)
                .Select(od => od.Order.UserId)
                .Distinct()
                .ToListAsync();

            if (cobuyers.Any())
            {
                // Collaborative filtering: products other buyers also purchased
                var combos = await _context.OrderDetails
                    .Include(od => od.Product).ThenInclude(p => p.Brand)
                    .Where(od => cobuyers.Contains(od.Order.UserId)
                                 && od.ProductId != productId
                                 && od.Product.IsActive)
                    .GroupBy(od => od.ProductId)
                    .OrderByDescending(g => g.Count())
                    .Take(count)
                    .Select(g => g.First().Product)
                    .ToListAsync();

                if (combos.Count > 0) return combos;
            }

            // Fallback: products from same category, different brand
            return await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.IsActive && p.Id != productId
                       && p.CategoryId == product.CategoryId
                       && p.BrandId != product.BrandId)
                .OrderByDescending(p => p.Reviews.Count)
                .Take(count)
                .ToListAsync();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // SHIPPING SYNC SERVICE (Mock)
    // ────────────────────────────────────────────────────────────────────────
    public class ShippingSyncService
    {
        private readonly ILogger<ShippingSyncService> _logger;
        private static readonly Random _rng = new();

        private static readonly string[] ShippingStatuses = {
            "Đã tiếp nhận đơn",
            "Đang đóng gói",
            "Đã bàn giao vận chuyển",
            "Đang trên đường giao",
            "Giao thành công"
        };

        public ShippingSyncService(ILogger<ShippingSyncService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Simulates fetching tracking status from a shipping carrier.
        /// In production: replace with GHN/GHTK/Viettel Post API call.
        /// </summary>
        public async Task<string> GetTrackingStatusAsync(string trackingCode)
        {
            // Simulate API latency
            await Task.Delay(200);

            // Deterministic fake status based on tracking code hash
            var idx = Math.Abs(trackingCode.GetHashCode()) % ShippingStatuses.Length;
            var status = ShippingStatuses[idx];

            _logger.LogInformation("🚚 [SHIPPING MOCK] Tracking {Code} → {Status}", trackingCode, status);
            return status;
        }

        public string GenerateTrackingCode(string orderCode)
        {
            return $"ML{DateTime.Now:MMddHHmm}{_rng.Next(1000, 9999)}";
        }
    }
}
