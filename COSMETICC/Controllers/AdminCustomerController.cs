using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminCustomerController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminCustomerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminCustomer
        public async Task<IActionResult> Index()
        {
            // Fetch users along with their Orders to calculate TotalSpent dynamically
            var customers = await _context.Users
                .Include(u => u.Orders)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(customers);
        }

        // POST: AdminCustomer/Lock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsLocked = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Khóa tài khoản {user.Username} thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminCustomer/Unlock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsLocked = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Mở khóa tài khoản {user.Username} thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminCustomer/History/5
        public async Task<IActionResult> History(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.CustomerName = user.FullName ?? user.Username;
            return View(orders);
        }
    }
}
