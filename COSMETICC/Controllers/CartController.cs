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
                return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để thực hiện tính năng này!" });
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
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity, int? productId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            int.TryParse(userIdStr, out int userId);

            CartItem? cartItem = null;
            if (cartItemId > 0)
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
            }
            else if (productId.HasValue && productId.Value > 0 && userId > 0)
            {
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart != null)
                {
                    cartItem = await _context.CartItems
                        .Include(ci => ci.Product)
                        .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId.Value);
                }
            }

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

            if (cartItem.Product != null && cartItem.Product.Stock < quantity)
            {
                return Json(new { success = false, message = $"Không đủ số lượng tồn kho (Còn {cartItem.Product.Stock} sản phẩm)!" });
            }

            cartItem.Quantity = quantity;
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật số lượng thành công!" });
        }

        // POST: /Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int? productId, int? cartItemId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            int.TryParse(userIdStr, out int userId);

            CartItem? cartItem = null;
            if (cartItemId.HasValue && cartItemId.Value > 0)
            {
                cartItem = await _context.CartItems.FindAsync(cartItemId.Value);
            }
            else if (productId.HasValue && productId.Value > 0 && userId > 0)
            {
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart != null)
                {
                    cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId.Value);
                }
            }

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Đã xóa mặt hàng khỏi giỏ hàng thành công!" });
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
        public async Task<IActionResult> Checkout(string? selectedIds)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiến hành đặt hàng!";
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Brand)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index");
            }

            // Filter cart items if selectedIds is provided
            if (!string.IsNullOrWhiteSpace(selectedIds))
            {
                var selectedIdList = selectedIds.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (selectedIdList.Any())
                {
                    cart.CartItems = cart.CartItems.Where(ci => selectedIdList.Contains(ci.Id)).ToList();
                }
            }

            if (!cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào được chọn để thanh toán!";
                return RedirectToAction("Index");
            }

            // Load user addresses to let them choose (Shopee style)
            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

            // Load available active vouchers
            var now = DateTime.Now;
            var availableVouchers = await _context.Discounts
                .Where(d => (d.ExpiryDate == null || d.ExpiryDate >= now) &&
                            (d.StartDate == null || d.StartDate <= now) &&
                            (d.UsageLimit == null || d.UsedCount < d.UsageLimit))
                .OrderByDescending(d => d.Percentage)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.Addresses = addresses;
            ViewBag.Vouchers = availableVouchers;
            ViewBag.SelectedIds = selectedIds ?? "";

            // Calculate initial shipping fee based on default address city & Admin config rules
            decimal initialShippingFee = 30000m;
            var defaultAddr = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();
            string userCity = defaultAddr?.City ?? user?.Address ?? "";

            if (!string.IsNullOrWhiteSpace(userCity))
            {
                decimal initialSubtotal = cart.CartItems.Sum(ci => (ci.Product.PromoPrice ?? ci.Product.Price) * (ci.Quantity ?? 1));
                var cleanCity = userCity.Trim().ToLower();
                var feeRules = await _context.ShippingFees.ToListAsync();
                var match = feeRules.FirstOrDefault(f => 
                    !string.IsNullOrWhiteSpace(f.Region) && 
                    (cleanCity.Contains(f.Region.Trim().ToLower()) || f.Region.Trim().ToLower().Contains(cleanCity)));

                if (match != null)
                {
                    initialShippingFee = (match.MinAmountForFreeShipping.HasValue && initialSubtotal >= match.MinAmountForFreeShipping.Value) ? 0m : match.Fee;
                }
                else if (initialSubtotal >= 299000m)
                {
                    initialShippingFee = 0m;
                }
            }
            ViewBag.InitialShippingFee = initialShippingFee;

            return View(cart);
        }

        // GET: /Cart/GetShippingFee
        [HttpGet]
        public async Task<IActionResult> GetShippingFee(string? city, decimal subtotal)
        {
            decimal fee = 30000m;
            if (!string.IsNullOrWhiteSpace(city))
            {
                var cleanCity = city.Trim().ToLower();
                var feeRules = await _context.ShippingFees.ToListAsync();
                var match = feeRules.FirstOrDefault(f => 
                    !string.IsNullOrWhiteSpace(f.Region) && 
                    (cleanCity.Contains(f.Region.Trim().ToLower()) || f.Region.Trim().ToLower().Contains(cleanCity)));

                if (match != null)
                {
                    if (match.MinAmountForFreeShipping.HasValue && subtotal >= match.MinAmountForFreeShipping.Value)
                    {
                        fee = 0m;
                    }
                    else
                    {
                        fee = match.Fee;
                    }
                }
                else if (subtotal >= 299000m)
                {
                    fee = 0m;
                }
            }
            else if (subtotal >= 299000m)
            {
                fee = 0m;
            }

            return Json(new { success = true, fee = fee, isFree = (fee == 0) });
        }

        // GET & POST: /Cart/ApplyVoucher (AJAX)
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string code, decimal? subtotal, string? selectedIds)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá!" });
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để áp dụng voucher!" });
            }

            // Calculate subtotal from cart if not passed or zero
            decimal cartSubtotal = subtotal ?? 0;
            if (cartSubtotal <= 0)
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null && cart.CartItems.Any())
                {
                    var items = cart.CartItems.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(selectedIds))
                    {
                        var selectedIdList = selectedIds.Split(',')
                            .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                            .Where(id => id > 0)
                            .ToList();
                        if (selectedIdList.Any())
                        {
                            items = items.Where(ci => selectedIdList.Contains(ci.Id));
                        }
                    }
                    cartSubtotal = items.Sum(ci => (ci.Product.PromoPrice ?? ci.Product.Price) * (ci.Quantity ?? 1));
                }
            }

            var cleanCode = code.Trim().ToLower();
            var now = DateTime.Now;

            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Code != null && d.Code.Trim().ToLower() == cleanCode);

            if (discount == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });
            }

            if (discount.StartDate.HasValue && discount.StartDate > now)
            {
                return Json(new { success = false, message = "Mã giảm giá chưa đến thời gian sử dụng!" });
            }

            if (discount.ExpiryDate.HasValue && discount.ExpiryDate < now)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn!" });
            }

            if (discount.UsageLimit.HasValue && discount.UsedCount >= discount.UsageLimit)
            {
                return Json(new { success = false, message = "Mã giảm giá đã hết số lượt sử dụng!" });
            }

            int percentage = discount.Percentage ?? 0;
            decimal discountAmount = Math.Round(cartSubtotal * percentage / 100m);

            return Json(new { 
                success = true, 
                discountId = discount.Id, 
                code = discount.Code, 
                percentage = percentage, 
                discountAmount = discountAmount,
                subtotal = cartSubtotal,
                message = $"Áp dụng voucher giảm {percentage}% thành công!" 
            });
        }

        // POST: /Cart/SaveNewAddress
        [HttpPost]
        public async Task<IActionResult> SaveNewAddress(string receiverName, string receiverPhone, string specificAddress, string city, bool isDefault = true)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện tính năng này!" });
            }

            if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(receiverPhone) || string.IsNullOrWhiteSpace(specificAddress))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ Tên, SĐT và Địa chỉ!" });
            }

            var userAddresses = await _context.UserAddresses.Where(a => a.UserId == userId).ToListAsync();

            if (isDefault)
            {
                foreach (var addr in userAddresses)
                {
                    addr.IsDefault = false;
                }
            }

            var newAddress = new UserAddress
            {
                UserId = userId,
                ReceiverName = receiverName.Trim(),
                ReceiverPhone = receiverPhone.Trim(),
                SpecificAddress = specificAddress.Trim(),
                City = string.IsNullOrWhiteSpace(city) ? "TP. Hồ Chí Minh" : city.Trim(),
                IsDefault = isDefault || !userAddresses.Any()
            };

            _context.UserAddresses.Add(newAddress);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                addressId = newAddress.Id,
                receiverName = newAddress.ReceiverName,
                receiverPhone = newAddress.ReceiverPhone,
                specificAddress = newAddress.SpecificAddress,
                city = newAddress.City,
                isDefault = newAddress.IsDefault,
                message = "Đã lưu địa chỉ mới làm địa chỉ mặc định thành công!"
            });
        }

        // POST: /Cart/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            string receiverName, 
            string receiverPhone, 
            string specificAddress, 
            string city, 
            string? notes, 
            string paymentMethod, 
            string? voucherCode,
            int? discountId,
            string? selectedIds,
            bool saveAddress = false)
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

            // Filter cart items if selectedIds is provided
            if (!string.IsNullOrWhiteSpace(selectedIds))
            {
                var selectedIdList = selectedIds.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (selectedIdList.Any())
                {
                    cart.CartItems = cart.CartItems.Where(ci => selectedIdList.Contains(ci.Id)).ToList();
                }
            }

            if (!cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào được chọn!";
                return RedirectToAction("Index");
            }

            var now = DateTime.Now;
            var activeFsItems = await _context.FlashSaleItems
                .Include(fsi => fsi.FlashSale)
                .Where(fsi => fsi.IsActive && fsi.SoldQuantity < fsi.QuantityForSale &&
                              fsi.FlashSale != null && fsi.FlashSale.IsActive &&
                              fsi.FlashSale.StartTime <= now && fsi.FlashSale.EndTime > now)
                .ToListAsync();

            // Subtotal
            decimal subtotal = cart.CartItems.Sum(ci =>
            {
                var fsItem = activeFsItems.FirstOrDefault(f => f.ProductId == ci.ProductId);
                decimal p = fsItem != null ? fsItem.DiscountPrice : (ci.Product.PromoPrice ?? ci.Product.Price);
                return p * (ci.Quantity ?? 1);
            });

            // Validate Voucher & Calculate Discount
            decimal discountAmount = 0;
            Discount? activeDiscount = null;

            if (discountId.HasValue && discountId.Value > 0)
            {
                activeDiscount = await _context.Discounts.FindAsync(discountId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                var cleanCode = voucherCode.Trim().ToLower();
                activeDiscount = await _context.Discounts.FirstOrDefaultAsync(d => d.Code != null && d.Code.Trim().ToLower() == cleanCode);
            }

            if (activeDiscount != null && 
                (activeDiscount.ExpiryDate == null || activeDiscount.ExpiryDate >= DateTime.Now) &&
                (activeDiscount.StartDate == null || activeDiscount.StartDate <= DateTime.Now) &&
                (activeDiscount.UsageLimit == null || activeDiscount.UsedCount < activeDiscount.UsageLimit))
            {
                int percentage = activeDiscount.Percentage ?? 0;
                discountAmount = Math.Round(subtotal * percentage / 100m);
            }

            // Shipping Fee calculation (Based on Admin configuration rules for destination city)
            decimal shippingFee = 30000m;
            if (!string.IsNullOrWhiteSpace(city))
            {
                var cleanCity = city.Trim().ToLower();
                var feeRules = await _context.ShippingFees.ToListAsync();
                var match = feeRules.FirstOrDefault(f => 
                    !string.IsNullOrWhiteSpace(f.Region) && 
                    (cleanCity.Contains(f.Region.Trim().ToLower()) || f.Region.Trim().ToLower().Contains(cleanCity)));

                if (match != null)
                {
                    shippingFee = (match.MinAmountForFreeShipping.HasValue && subtotal >= match.MinAmountForFreeShipping.Value) ? 0m : match.Fee;
                }
                else if (subtotal >= 299000m)
                {
                    shippingFee = 0m;
                }
            }
            else if (subtotal >= 299000m)
            {
                shippingFee = 0m;
            }

            decimal finalTotal = Math.Max(0, subtotal + shippingFee - discountAmount);

            // Save new address to UserAddresses if requested
            if (saveAddress && !string.IsNullOrWhiteSpace(specificAddress))
            {
                var existingAddrs = await _context.UserAddresses.Where(a => a.UserId == userId).ToListAsync();
                var exists = existingAddrs.FirstOrDefault(a => a.SpecificAddress == specificAddress && a.ReceiverName == receiverName);
                
                if (exists == null)
                {
                    bool makeDefault = !existingAddrs.Any(a => a.IsDefault);
                    if (makeDefault)
                    {
                        foreach (var a in existingAddrs) { a.IsDefault = false; }
                    }

                    var newAddr = new UserAddress
                    {
                        UserId = userId,
                        ReceiverName = receiverName,
                        ReceiverPhone = receiverPhone,
                        SpecificAddress = specificAddress,
                        City = city,
                        IsDefault = makeDefault
                    };
                    _context.UserAddresses.Add(newAddr);
                    await _context.SaveChangesAsync();
                }
            }

            // Start Transaction to guarantee database consistency (FIFO)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Check Fraud Rules
                var fraudCheck = await _fraudService.CheckOrderAsync(userId, finalTotal);
                if (fraudCheck.IsSuspect)
                {
                    var adminEmail = _context.Admins.FirstOrDefault(a => a.Role == "ADMIN")?.Email ?? "admin@meilingcosmetics.vn";
                    await _emailService.SendFraudAlertToAdminAsync(
                        adminEmail,
                        0,
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
                    TotalAmount = finalTotal,
                    Status = "Chờ xác nhận",
                    OrderCode = orderCode,
                    Notes = notes
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 4. Save Discount usage if applied
                if (activeDiscount != null)
                {
                    var orderDiscount = new OrderDiscount
                    {
                        OrderId = order.Id,
                        DiscountId = activeDiscount.Id
                    };
                    _context.OrderDiscounts.Add(orderDiscount);

                    activeDiscount.UsedCount += 1;
                    _context.Discounts.Update(activeDiscount);

                    // Check if collected voucher exists and mark as used
                    var collected = await _context.CollectedVouchers
                        .FirstOrDefaultAsync(cv => cv.UserId == userId && cv.DiscountId == activeDiscount.Id && !cv.IsUsed);
                    if (collected != null)
                    {
                        collected.IsUsed = true;
                        _context.CollectedVouchers.Update(collected);
                    }
                }

                // 5. Create Shipping Info
                var shipping = new Shipping
                {
                    OrderId = order.Id,
                    Address = $"{specificAddress}, {city}",
                    Phone = receiverPhone,
                    ShippingStatus = "Chờ xác nhận",
                    ShippingDate = null
                };
                _context.Shippings.Add(shipping);

                // 6. Create Payment Info
                var payment = new Payment
                {
                    OrderId = order.Id,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = paymentMethod == "VNPay" ? "Chờ thanh toán" : "Chưa thanh toán",
                    PaymentDate = null
                };
                _context.Payments.Add(payment);

                // 7. Add Order Details & Apply FIFO warehouse deduction
                string itemsSummaryHtml = "";
                foreach (var cartItem in cart.CartItems)
                {
                    var product = cartItem.Product;
                    int qtyOrdered = cartItem.Quantity ?? 1;

                    var fsItem = activeFsItems.FirstOrDefault(f => f.ProductId == cartItem.ProductId);
                    decimal price = fsItem != null ? fsItem.DiscountPrice : (product.PromoPrice ?? product.Price);

                    if (fsItem != null)
                    {
                        fsItem.SoldQuantity += qtyOrdered;
                        if (fsItem.SoldQuantity >= fsItem.QuantityForSale)
                        {
                            fsItem.IsActive = false;
                        }
                        _context.FlashSaleItems.Update(fsItem);
                    }

                    itemsSummaryHtml += $"• {product.Name} (x{qtyOrdered}) - {price:N0}₫<br/>";

                    if (product.Stock < qtyOrdered)
                    {
                        throw new Exception($"Sản phẩm '{product.Name}' không đủ số lượng trong kho (Còn {product.Stock} sản phẩm).");
                    }

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

                    product.Stock -= qtyOrdered;
                    _context.Products.Update(product);
                }

                // Clear Cart Items
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                // Update Product Expiry Dates based on active remaining batches
                foreach (var cartItem in cart.CartItems)
                {
                    await Services.WarehouseHelper.UpdateProductExpiryDateAsync(_context, cartItem.ProductId);
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // 8. Handle Payment Method Redirect
                if (paymentMethod == "VNPay")
                {
                    // Build VNPay Payment URL directly for this order
                    var vnpayConfig = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                    var vnpay = new Libraries.VnPayLibrary();

                    vnpay.AddRequestData("vnp_Version", vnpayConfig["VnPay:Version"] ?? "2.1.0");
                    vnpay.AddRequestData("vnp_Command", vnpayConfig["VnPay:Command"] ?? "pay");
                    vnpay.AddRequestData("vnp_TmnCode", vnpayConfig["VnPay:TmnCode"]);

                    long amount = (long)(finalTotal * 100);
                    vnpay.AddRequestData("vnp_Amount", amount.ToString());
                    vnpay.AddRequestData("vnp_CreateDate", order.OrderDate.Value.ToString("yyyyMMddHHmmss"));
                    vnpay.AddRequestData("vnp_CurrCode", vnpayConfig["VnPay:CurrCode"] ?? "VND");
                    
                    string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    vnpay.AddRequestData("vnp_IpAddr", ipAddress);
                    vnpay.AddRequestData("vnp_Locale", vnpayConfig["VnPay:Locale"] ?? "vn");
                    vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
                    vnpay.AddRequestData("vnp_OrderType", "other");

                    string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/PaymentCallback";
                    vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);

                    string tick = DateTime.Now.Ticks.ToString();
                    vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString() + "_" + tick);

                    string paymentUrl = vnpay.CreateRequestUrl(vnpayConfig["VnPay:BaseUrl"], vnpayConfig["VnPay:HashSecret"]);
                    return Redirect(paymentUrl);
                }
                else if (paymentMethod == "MoMo")
                {
                    var momoService = HttpContext.RequestServices.GetRequiredService<Services.IMomoService>();
                    string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/MomoReturn";
                    string ipnUrl = $"{Request.Scheme}://{Request.Host}/Payment/MomoNotify";

                    var momoResponse = await momoService.CreatePaymentAsync(order, returnUrl, ipnUrl);
                    if (momoResponse != null && !string.IsNullOrEmpty(momoResponse.PayUrl))
                    {
                        return Redirect(momoResponse.PayUrl);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = momoResponse?.Message ?? "Không thể kết nối cổng thanh toán MoMo!";
                        return RedirectToAction("OrderSuccess", new { id = order.Id });
                    }
                }

                // 9. COD Order Notifications
                try
                {
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendOrderConfirmationAsync(
                            user.Email,
                            user.FullName ?? user.Username,
                            orderCode,
                            finalTotal,
                            itemsSummaryHtml
                        );
                    }
                }
                catch { }

                await _notificationService.SendAsync(
                    userId,
                    "🛒 Đặt hàng thành công",
                    $"Đơn hàng #{orderCode} trị giá {finalTotal:N0}₫ đã được ghi nhận.",
                    "order",
                    "/Account/MyOrders"
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
