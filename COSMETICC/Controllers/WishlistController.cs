using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;

namespace COSMETICC.Controllers
{
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Wishlist
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                // Redirect to Login if guest tries to view wishlist
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                .ThenInclude(p => p.Brand)
                .Include(w => w.Product)
                .ThenInclude(p => p.Reviews)
                .Where(w => w.UserId == userId && w.Product.IsActive)
                .Select(w => w.Product)
                .ToListAsync();

            return View(wishlistItems);
        }

        // POST: /Wishlist/ToggleWishlist
        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để thêm sản phẩm vào danh sách yêu thích!" });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            bool isWishlisted;
            string message;

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                isWishlisted = false;
                message = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
            }
            else
            {
                var newWish = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId
                };
                _context.Wishlists.Add(newWish);
                isWishlisted = true;
                message = "Đã thêm sản phẩm vào danh sách yêu thích!";
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, isWishlisted, message });
        }

        // GET: /Wishlist/GetUserWishlist
        [HttpGet]
        public async Task<IActionResult> GetUserWishlist()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new int[0]);
            }

            var ids = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            return Json(ids);
        }
    }
}
