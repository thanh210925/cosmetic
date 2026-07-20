using COSMETICC.Models;
using COSMETICC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace Cosmetic.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
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
        public async Task<IActionResult> Register(User user)
        {
            // check email tồn tại
            var exist = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == user.Email);

            if (exist != null)
            {
                ViewBag.Error = "Email đã tồn tại";
                return View();
            }

            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // ================= LOGIN (EMAIL / PHONE) =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string input, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == input || u.Phone == input);

            if (user != null && user.Password == password)
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("FullName", user.FullName ?? "");
                HttpContext.Session.SetString("Email", user.Email ?? "");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai thông tin đăng nhập";
            return View();
        }

        // ================= LOGIN GOOGLE =================

        public IActionResult LoginGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync();

            if (!result.Succeeded || result.Principal == null)
            {
                return RedirectToAction("Login");
            }

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);

            // nếu chưa có → tạo mới
            if (user == null)
            {
                user = new User
                {
                    Username = email.Split('@')[0],
                    Email = email,
                    FullName = name,
                    Password = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // lưu session
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("Email", user.Email ?? "");

            return RedirectToAction("Index", "Home");
        }
        // ================= PROFILE =================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.UserAddresses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Ensure address list is ordered by IsDefault descending
            user.UserAddresses = user.UserAddresses.OrderByDescending(a => a.IsDefault).ToList();

            return View(user);
        }
        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ================= LOYALTY & VOUCHERS =================

        [HttpGet]
        public async Task<IActionResult> Loyalty()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Fetch active vouchers
            var now = DateTime.Now;
            var activeDiscounts = await _context.Discounts
                .Where(d => d.StartDate <= now && d.ExpiryDate >= now && d.UsageLimit > d.UsedCount)
                .ToListAsync();

            // Fetch collected vouchers for this user
            var collectedVouchers = await _context.CollectedVouchers
                .Include(cv => cv.Discount)
                .Where(cv => cv.UserId == userId)
                .ToListAsync();

            // Distinguish available vs collected
            var collectedDiscountIds = collectedVouchers.Select(cv => cv.DiscountId).ToList();
            var availableVouchers = activeDiscounts
                .Where(d => !collectedDiscountIds.Contains(d.Id))
                .ToList();

            ViewBag.AvailableVouchers = availableVouchers;
            ViewBag.CollectedVouchers = collectedVouchers;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CollectVoucher(int discountId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thu thập voucher!" });
            }

            var discount = await _context.Discounts.FindAsync(discountId);
            if (discount == null)
            {
                return Json(new { success = false, message = "Voucher không tồn tại!" });
            }

            var now = DateTime.Now;
            if (discount.StartDate > now || discount.ExpiryDate < now || discount.UsedCount >= discount.UsageLimit)
            {
                return Json(new { success = false, message = "Voucher này đã hết hạn hoặc hết lượt sử dụng!" });
            }

            // Check if already collected
            var alreadyCollected = await _context.CollectedVouchers
                .AnyAsync(cv => cv.UserId == userId && cv.DiscountId == discountId);

            if (alreadyCollected)
            {
                return Json(new { success = false, message = "Bạn đã thu thập voucher này rồi!" });
            }

            var collected = new CollectedVoucher
            {
                UserId = userId,
                DiscountId = discountId,
                CollectedAt = DateTime.Now,
                IsUsed = false
            };

            _context.CollectedVouchers.Add(collected);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thu thập voucher thành công! Đã lưu vào ví voucher của bạn." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBirthday(DateTime birthDate)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Optional: check if birthday was already set to prevent users changing it multiple times for multiple vouchers
            if (user.BirthDate.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn chỉ được thiết lập ngày sinh một lần duy nhất!";
                return RedirectToAction("Loyalty");
            }

            user.BirthDate = birthDate;
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Give a special birthday voucher immediately if birthday month matches current month!
            // Let's create a birthday discount programmatically if it doesn't exist
            var bdayCode = $"BDAY-{user.Id}-{DateTime.Now.Year}";
            var existingBdayVoucher = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == bdayCode);

            if (existingBdayVoucher == null)
            {
                var discount = new Discount
                {
                    Code = bdayCode,
                    Percentage = 20, // 20% off
                    StartDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddDays(30), // Valid for 30 days
                    UsageLimit = 1,
                    UsedCount = 0
                };
                _context.Discounts.Add(discount);
                await _context.SaveChangesAsync();

                var collected = new CollectedVoucher
                {
                    UserId = userId,
                    DiscountId = discount.Id,
                    CollectedAt = DateTime.Now,
                    IsUsed = false
                };
                _context.CollectedVouchers.Add(collected);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật ngày sinh thành công! Hệ thống đã tặng bạn 1 Voucher Sinh Nhật giảm giá 20% trong ví!";
            }
            else
            {
                TempData["SuccessMessage"] = "Cập nhật ngày sinh thành công!";
            }

            return RedirectToAction("Loyalty");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Validate duplicate email if email is changed
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExist = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
                if (emailExist)
                {
                    TempData["ErrorMessage"] = "Email này đã được sử dụng bởi tài khoản khác!";
                    return RedirectToAction("Profile");
                }
            }

            // Update allowed fields
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.BirthDate = model.BirthDate;
            user.Avatar = model.Avatar;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Update session values
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("Email", user.Email ?? "");

            TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmNewPassword)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmNewPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp!";
                return RedirectToAction("Profile");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Password != oldPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác!";
                return RedirectToAction("Profile");
            }

            user.Password = newPassword;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // ================= ADDRESS MANAGEMENT =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(UserAddress address)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            address.UserId = userId;

            if (address.IsDefault)
            {
                // Unset other default addresses
                var currentDefaults = await _context.UserAddresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();
                foreach (var cur in currentDefaults)
                {
                    cur.IsDefault = false;
                }
            }
            else
            {
                // If this is the only address, set it as default
                var hasAnyAddress = await _context.UserAddresses.AnyAsync(a => a.UserId == userId);
                if (!hasAnyAddress)
                {
                    address.IsDefault = true;
                }
            }

            _context.UserAddresses.Add(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm địa chỉ giao hàng mới thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(int id, UserAddress model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null) return NotFound();

            address.ReceiverName = model.ReceiverName;
            address.ReceiverPhone = model.ReceiverPhone;
            address.SpecificAddress = model.SpecificAddress;
            address.City = model.City;

            if (model.IsDefault && !address.IsDefault)
            {
                // Set default and unset others
                var currentDefaults = await _context.UserAddresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();
                foreach (var cur in currentDefaults)
                {
                    cur.IsDefault = false;
                }
                address.IsDefault = true;
            }

            _context.UserAddresses.Update(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null) return NotFound();

            bool wasDefault = address.IsDefault;

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();

            // If deleted address was default, set another one as default if exists
            if (wasDefault)
            {
                var another = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.UserId == userId);
                if (another != null)
                {
                    another.IsDefault = true;
                    _context.UserAddresses.Update(another);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Đã xóa địa chỉ giao hàng thành công.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null) return NotFound();

            // Unset all other defaults
            var currentDefaults = await _context.UserAddresses
                .Where(a => a.UserId == userId && a.IsDefault && a.Id != id)
                .ToListAsync();

            foreach (var cur in currentDefaults)
            {
                cur.IsDefault = false;
                _context.UserAddresses.Update(cur);
            }

            address.IsDefault = true;
            _context.UserAddresses.Update(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã đặt địa chỉ làm mặc định.";
            return RedirectToAction("Profile");
        }

        // ================= MY ORDERS =================

        [HttpGet]
        public async Task<IActionResult> MyOrders(string? status, string? search, int page = 1)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login");

            int pageSize = 5;

            var baseQuery = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Shippings)
                .Include(o => o.Payments)
                .Where(o => o.UserId == userId);

            // Compute counts for tabs
            ViewBag.AllCount = await baseQuery.CountAsync();
            ViewBag.PendingCount = await baseQuery.CountAsync(o => o.Status == "Chờ xác nhận" || o.Status == "Pending");
            ViewBag.ConfirmedCount = await baseQuery.CountAsync(o => o.Status == "Đã xác nhận" || o.Status == "Confirmed" || o.Status == "Đang đóng gói");
            ViewBag.ShippingCount = await baseQuery.CountAsync(o => o.Status == "Bàn giao cho GHN" || o.Status == "Đang giao hàng" || o.Status == "Shipping");
            ViewBag.DeliveredCount = await baseQuery.CountAsync(o => o.Status == "Đã giao hàng" || o.Status == "Hoàn thành" || o.Status == "Delivered" || o.Status == "Completed");
            ViewBag.CancelledCount = await baseQuery.CountAsync(o => o.Status == "Hủy" || o.Status == "Đã hủy" || o.Status == "Cancelled" || o.Status == "Failed");

            var query = baseQuery.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var s = status.Trim().ToUpper();
                if (s == "PENDING" || s == "CHỜ XÁC NHẬN")
                {
                    query = query.Where(o => o.Status == "Chờ xác nhận" || o.Status == "Pending");
                }
                else if (s == "CONFIRMED" || s == "ĐÃ XÁC NHẬN")
                {
                    query = query.Where(o => o.Status == "Đã xác nhận" || o.Status == "Confirmed" || o.Status == "Đang đóng gói");
                }
                else if (s == "SHIPPING" || s == "ĐANG GIAO")
                {
                    query = query.Where(o => o.Status == "Bàn giao cho GHN" || o.Status == "Đang giao hàng" || o.Status == "Shipping");
                }
                else if (s == "DELIVERED" || s == "ĐÃ GIAO")
                {
                    query = query.Where(o => o.Status == "Đã giao hàng" || o.Status == "Hoàn thành" || o.Status == "Delivered" || o.Status == "Completed");
                }
                else if (s == "CANCELLED" || s == "HỦY" || s == "ĐÃ HỦY")
                {
                    query = query.Where(o => o.Status == "Hủy" || o.Status == "Đã hủy" || o.Status == "Cancelled" || o.Status == "Failed");
                }
                else
                {
                    query = query.Where(o => o.Status == status);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderCode != null && o.OrderCode.Contains(search));
            }

            int total = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalOrders = total;

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            // Only allow cancellation if order is still pending
            if (order.Status != "Pending" && order.Status != "Chờ xác nhận")
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng ở trạng thái Chờ xác nhận.";
                return RedirectToAction("MyOrders");
            }

            order.Status = "Hủy";
            order.CancelledAt = DateTime.Now;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // Restore product stock and batch remaining quantities
            await WarehouseHelper.RestoreOrderStockAsync(_context, order.Id);

            TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{order.OrderCode} và hoàn trả số lượng tồn kho thành công.";
            return RedirectToAction("MyOrders");
        }
    }
}