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

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

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

                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        // ================= DASHBOARD =================

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

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