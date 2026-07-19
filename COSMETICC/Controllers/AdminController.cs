using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace Cosmetic.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }
        // ================= REGISTER =================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Admin admin)
        {
            // ❗ CHECK EMAIL PHẢI CÓ "ml"
            if (string.IsNullOrEmpty(admin.Email) || !admin.Email.ToLower().Contains("ml"))
            {
                ViewBag.Error = "Email phải chứa 'ml'";
                return View(admin);
            }

            // check trùng username
            var userExist = await _context.Admins
                .FirstOrDefaultAsync(x => x.Username == admin.Username);

            if (userExist != null)
            {
                ViewBag.Error = "Username đã tồn tại";
                return View(admin);
            }

            // check trùng email
            var emailExist = await _context.Admins
                .FirstOrDefaultAsync(x => x.Email == admin.Email);

            if (emailExist != null)
            {
                ViewBag.Error = "Email đã tồn tại";
                return View(admin);
            }

            admin.CreatedAt = DateTime.Now;

            try
            {
                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                ViewBag.Error = $"Lỗi cơ sở dữ liệu: {innerMsg}";
                return View(admin);
            }

            return RedirectToAction("Login");
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == username);

            if (admin != null && admin.Password == password)
            {
                HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                HttpContext.Session.SetString("AdminName", admin.FullName ?? "");
                HttpContext.Session.SetString("AdminRole", admin.Role ?? "ADMIN");

                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        // ================= DASHBOARD =================

        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            // Standard totals
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalCustomers = await _context.Users.CountAsync();

            var totalSales = await _context.Orders
                .Where(o => o.Status != "Hủy")
                .SumAsync(o => o.TotalAmount);
            ViewBag.TotalSales = totalSales ?? 0;

            // Today's Stats
            ViewBag.TodayOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= DateTime.Today);

            var todaySales = await _context.Orders
                .Where(o => o.OrderDate >= DateTime.Today && o.Status != "Hủy")
                .SumAsync(o => o.TotalAmount);
            ViewBag.TodaySales = todaySales ?? 0;

            // Customer registrations last 7 days
            ViewBag.NewCustomersWeek = await _context.Users
                .CountAsync(u => u.CreatedAt >= DateTime.Today.AddDays(-7));

            // Low Stock products alert (Stock <= 10)
            ViewBag.LowStockCount = await _context.Products
                .CountAsync(p => p.Stock <= 10);

            // Products Expiration Alerts (ExpiryDate under 6 months from now)
            var sixMonthsFromNow = DateTime.Today.AddMonths(6);
            ViewBag.ExpiryAlerts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.ExpiryDate != null && p.ExpiryDate <= sixMonthsFromNow)
                .OrderBy(p => p.ExpiryDate)
                .Take(5)
                .ToListAsync();

            // Recent Lists
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Top Selling products names and quantities
            var topSelling = await _context.OrderDetails
                .Include(d => d.Product)
                .GroupBy(d => d.Product.Name)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity ?? 0) })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToListAsync();
            
            ViewBag.TopSellingNames = topSelling.Select(x => x.Name).ToList();
            ViewBag.TopSellingQtys = topSelling.Select(x => x.Qty).ToList();

            // 7-day Sales Revenue history for charts
            var salesData = new List<decimal>();
            var salesLabels = new List<string>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var nextDate = date.AddDays(1);
                var daySales = await _context.Orders
                    .Where(o => o.OrderDate >= date && o.OrderDate < nextDate && o.Status != "Hủy")
                    .SumAsync(o => o.TotalAmount);
                
                salesData.Add(daySales ?? 0);
                salesLabels.Add(date.ToString("dd/MM"));
            }
            ViewBag.SalesData = salesData;
            ViewBag.SalesLabels = salesLabels;

            return View();
        }

        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminId");
            HttpContext.Session.Remove("AdminName");

            return RedirectToAction("Login");
        }
    }
}