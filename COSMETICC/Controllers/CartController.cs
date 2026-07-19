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
        private readonly Services.OrderCodeService _orderCodeService;
        private readonly Services.EmailService _emailService;
        private readonly Services.NotificationService _notificationService;
        private readonly Services.FraudDetectionService _fraudService;

        public CartController(
            AppDbContext context,
            Services.OrderCodeService orderCodeService,
            Services.EmailService emailService,
            Services.NotificationService notificationService,
            Services.FraudDetectionService fraudService)
        {
            _context = context;
            _orderCodeService = orderCodeService;
            _emailService = emailService;
            _notificationService = notificationService;
            _fraudService = fraudService;
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

        // GET: /Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiến hành đặt hàng!";
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            // Load user addresses to let them choose
            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            ViewBag.Addresses = addresses;
            return View(cart);
        }

        // POST: /Cart/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string receiverName, string receiverPhone, string specificAddress, string city, string? notes, string paymentMethod)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng rỗng!";
                return RedirectToAction("Index");
            }

            // Calculate Total Amount
            decimal totalAmount = cart.CartItems.Sum(ci => (ci.Product.PromoPrice ?? ci.Product.Price) * (ci.Quantity ?? 1));

            // Start Transaction to guarantee database consistency (FIFO)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Check Fraud Rules
                var fraudCheck = await _fraudService.CheckOrderAsync(userId, totalAmount);
                if (fraudCheck.IsSuspect)
                {
                    // Raise admin alert via Email / Log
                    var adminEmail = _context.Admins.FirstOrDefault(a => a.Role == "ADMIN")?.Email ?? "admin@meilingcosmetics.vn";
                    await _emailService.SendFraudAlertToAdminAsync(
                        adminEmail,
                        0, // Will update actual ID after saving
                        fraudCheck.Reason,
                        $"Khách hàng: {user.FullName} ({user.Username}) - SĐT: {receiverPhone}"
                    );
                }

                // 2. Generate unique order code
                string orderCode = _orderCodeService.Generate();

                // 3. Create Order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Status = "Chờ xác nhận", // Trạng thái ban đầu
                    OrderCode = orderCode,
                    Notes = notes
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 4. Create Shipping Info
                var shipping = new Shipping
                {
                    OrderId = order.Id,
                    Address = $"{specificAddress}, {city}",
                    Phone = receiverPhone,
                    ShippingStatus = "Đang chuẩn bị hàng",
                    ShippingDate = null
                };
                _context.Shippings.Add(shipping);

                // 5. Create Payment Info
                var payment = new Payment
                {
                    OrderId = order.Id,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "Chưa thanh toán",
                    PaymentDate = null
                };
                _context.Payments.Add(payment);

                // 6. Add Order Details & Apply FIFO warehouse deduction
                string itemsSummaryHtml = "";
                foreach (var cartItem in cart.CartItems)
                {
                    var product = cartItem.Product;
                    int qtyOrdered = cartItem.Quantity ?? 1;
                    decimal price = product.PromoPrice ?? product.Price;

                    itemsSummaryHtml += $"• {product.Name} (x{qtyOrdered}) - {price:N0}₫<br/>";

                    // Check stock first
                    if (product.Stock < qtyOrdered)
                    {
                        throw new Exception($"Sản phẩm '{product.Name}' không đủ số lượng trong kho (Còn {product.Stock} sản phẩm).");
                    }

                    // OrderDetail record
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = qtyOrdered,
                        Price = price
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // FIFO DEDUCTION ALGORITHM
                    int qtyNeeded = qtyOrdered;

                    // Fetch active batches for this product with RemainingQuantity > 0 ordered by ExpiryDate ASC, ImportDate ASC
                    var activeBatches = await _context.ProductBatches
                        .Where(b => b.ProductId == cartItem.ProductId && b.RemainingQuantity > 0 && b.ExpiryDate > DateTime.Now)
                        .OrderBy(b => b.ExpiryDate)
                        .ThenBy(b => b.ImportDate)
                        .ToListAsync();

                    int totalBatchQtyAvailable = activeBatches.Sum(b => b.RemainingQuantity);
                    if (totalBatchQtyAvailable < qtyNeeded)
                    {
                        throw new Exception($"Không đủ tồn kho khả dụng theo lô cho sản phẩm '{product.Name}' (Lô khả dụng còn {totalBatchQtyAvailable}).");
                    }

                    foreach (var batch in activeBatches)
                    {
                        if (qtyNeeded <= 0) break;

                        int qtyDeducted = 0;
                        if (batch.RemainingQuantity >= qtyNeeded)
                        {
                            qtyDeducted = qtyNeeded;
                            batch.RemainingQuantity -= qtyNeeded;
                            qtyNeeded = 0;
                        }
                        else
                        {
                            qtyDeducted = batch.RemainingQuantity;
                            qtyNeeded -= batch.RemainingQuantity;
                            batch.RemainingQuantity = 0;
                        }

                        _context.ProductBatches.Update(batch);

                        // Ghi nhận log kho xuất chi tiết cho lô hàng này
                        var log = new InventoryLog
                        {
                            ProductId = product.Id,
                            Type = "SELL",
                            Quantity = qtyDeducted,
                            BatchNumber = batch.BatchNumber,
                            ReferenceCode = orderCode,
                            Note = $"Xuất bán đơn hàng #{orderCode} | Trừ từ lô: {batch.BatchNumber}",
                            CreatedAt = DateTime.Now
                        };
                        _context.InventoryLogs.Add(log);
                    }

                    // Deduct main Product total stock
                    product.Stock -= qtyOrdered;
                    _context.Products.Update(product);
                }

                // 7. Clear Cart Items
                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();

                // 8. Update Product Expiry Dates based on active remaining batches
                foreach (var cartItem in cart.CartItems)
                {
                    await Services.WarehouseHelper.UpdateProductExpiryDateAsync(_context, cartItem.ProductId);
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // 9. Trigger Notifications (Fail-safe email sending)
                try
                {
                    // Send Email Confirmation to customer
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendOrderConfirmationAsync(
                            user.Email,
                            user.FullName ?? user.Username,
                            orderCode,
                            totalAmount,
                            itemsSummaryHtml
                        );
                    }
                }
                catch { /* Suppress email errors to prevent transaction crash */ }

                // In-app Notification to customer
                await _notificationService.SendAsync(
                    userId,
                    "🛒 Đặt hàng thành công",
                    $"Đơn hàng #{orderCode} trị giá {totalAmount:N0}₫ đã được ghi nhận.",
                    "order",
                    "/Cart/OrderHistory"
                );

                TempData["SuccessMessage"] = $"🎉 Đặt hàng thành công! Mã đơn của bạn là <strong>#{orderCode}</strong>. Email xác nhận đã được gửi.";
                return RedirectToAction("OrderSuccess", new { orderCode = orderCode });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Đặt hàng không thành công: {ex.Message}";
                return RedirectToAction("Checkout");
            }
        }

        // GET: /Cart/OrderSuccess
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(string orderCode)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: /Cart/OrderHistory
        [HttpGet]
        public async Task<IActionResult> OrderHistory()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}
