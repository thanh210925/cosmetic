using System;
using System.Linq;
using System.Threading.Tasks;
using COSMETICC.Models;
using COSMETICC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Controllers
{
    public class AutomationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;
        private readonly FraudDetectionService _fraudService;
        private readonly PdfInvoiceService _pdfService;
        private readonly OrderCodeService _orderCodeService;

        public AutomationController(
            AppDbContext context,
            EmailService emailService,
            NotificationService notificationService,
            FraudDetectionService fraudService,
            PdfInvoiceService pdfService,
            OrderCodeService orderCodeService)
        {
            _context = context;
            _emailService = emailService;
            _notificationService = notificationService;
            _fraudService = fraudService;
            _pdfService = pdfService;
            _orderCodeService = orderCodeService;
        }

        private bool IsAdmin()
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            return !string.IsNullOrEmpty(adminId);
        }

        // ─── Dashboard ────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Admin");

            // Stats
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Chờ xác nhận" || o.Status == "Pending");
            var cancelledToday = await _context.Orders.CountAsync(o => o.CancelledAt != null && o.CancelledAt >= DateTime.Today);
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var unreadNotifs = await _context.Notifications.CountAsync(n => !n.IsRead);
            var recentNotifs = await _context.Notifications
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Fraud suspects — orders with very high value
            var fraudSuspects = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.TotalAmount >= 10_000_000m && o.Status != "Đã hủy (Quá hạn)")
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            ViewBag.PendingOrders = pendingOrders;
            ViewBag.CancelledToday = cancelledToday;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.UnreadNotifs = unreadNotifs;
            ViewBag.RecentNotifs = recentNotifs;
            ViewBag.FraudSuspects = fraudSuspects;

            return View();
        }

        // ─── Download Invoice PDF ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadInvoice(int orderId)
        {
            // Allow admin or the order owner
            var adminId = HttpContext.Session.GetString("AdminId");
            var userIdStr = HttpContext.Session.GetString("UserId");

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            // Auth check
            if (string.IsNullOrEmpty(adminId))
            {
                if (!int.TryParse(userIdStr, out int uid) || order.UserId != uid)
                    return Forbid();
            }

            var pdfBytes = _pdfService.GenerateInvoicePdf(order, order.User, order.OrderDetails.ToList());
            var fileName = $"HoaDon-{order.OrderCode ?? order.Id.ToString()}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        // ─── Manual: Send test email ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> TestEmail(string toEmail, string toName)
        {
            if (!IsAdmin()) return Unauthorized();

            await _emailService.SendOrderConfirmationAsync(
                toEmail, toName ?? "Test",
                _orderCodeService.Generate(),
                299000,
                "• Serum Niacinamide 10% (x1)<br/>• Kem dưỡng ẩm Ceramide (x2)"
            );

            TempData["SuccessMessage"] = $"📧 Email test đã gửi đến {toEmail} (kiểm tra Console nếu chưa cấu hình SMTP)";
            return RedirectToAction("Dashboard");
        }

        // ─── Manual: Broadcast notification ──────────────────────────────
        [HttpPost]
        public async Task<IActionResult> BroadcastNotification(string title, string message, string type)
        {
            if (!IsAdmin()) return Unauthorized();

            // Broadcast to all users
            var userIds = await _context.Users.Select(u => u.Id).ToListAsync();
            foreach (var uid in userIds)
            {
                await _notificationService.SendAsync(uid, title, message, type ?? "system");
            }

            TempData["SuccessMessage"] = $"🔔 Đã gửi thông báo đến {userIds.Count} người dùng";
            return RedirectToAction("Dashboard");
        }

        // ─── Get user notifications (AJAX) ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdStr, out int userId))
                return Json(new { count = 0, items = Array.Empty<object>() });

            var notifs = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new {
                    n.Id, n.Title, n.Message, n.Type, n.IsRead, n.Link,
                    Time = n.CreatedAt.ToString("dd/MM HH:mm")
                })
                .ToListAsync();

            var unread = notifs.Count(n => !n.IsRead);
            return Json(new { count = unread, items = notifs });
        }

        // ─── Mark notifications read ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> MarkNotificationsRead()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdStr, out int userId))
                return Json(new { success = false });

            await _notificationService.MarkAllReadAsync(userId);
            return Json(new { success = true });
        }

        // ─── Mark single notification read ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> MarkOneRead(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
    }
}
