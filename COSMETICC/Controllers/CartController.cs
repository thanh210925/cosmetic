using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;

namespace COSMETICC.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem giỏ hàng!";
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Brand)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện tính năng này!" });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            if (product.Stock < quantity)
            {
                return Json(new { success = false, message = $"Số lượng sản phẩm trong kho không đủ (Hiện còn {product.Stock} sản phẩm)!" });
            }

            // Get or create cart
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Get or create cart item
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
                _context.CartItems.Update(cartItem);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng thành công!" });
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Mặt hàng không tồn tại trong giỏ!" });
            }

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa mặt hàng khỏi giỏ hàng." });
            }

            if (cartItem.Product.Stock < quantity)
            {
                return Json(new { success = false, message = $"Không đủ số lượng tồn kho (Còn {cartItem.Product.Stock} sản phẩm)!" });
            }

            cartItem.Quantity = quantity;
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật số lượng thành công!" });
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
            {
                return Json(new { success = false, message = "Mặt hàng không tồn tại trong giỏ!" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa mặt hàng khỏi giỏ hàng thành công!" });
        }
        // GET: /Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { count = 0 });

            var count = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => (int?)ci.Quantity) ?? 0;

            return Json(new { count });
        }

        // POST: /Cart/SetProductQuantity
        [HttpPost]
        public async Task<IActionResult> SetProductQuantity(int productId, int quantity)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện tính năng này!" });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (quantity <= 0)
            {
                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true, count = 0, message = "Đã xóa khỏi giỏ hàng!" });
            }

            if (product.Stock < quantity)
            {
                return Json(new { success = false, message = $"Số lượng sản phẩm trong kho không đủ (Hiện còn {product.Stock} sản phẩm)!" });
            }

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
                _context.CartItems.Update(cartItem);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, count = quantity, message = "Cập nhật giỏ hàng thành công!" });
        }

        // GET: /Cart/GetCartItems
        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new List<object>());
            }

            var items = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .Select(ci => new { ci.ProductId, ci.Quantity })
                .ToListAsync();

            return Json(items);
        }
    }
}
