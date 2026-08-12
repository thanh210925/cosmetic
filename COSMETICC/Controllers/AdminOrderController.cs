using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;
using COSMETICC.Services;

namespace COSMETICC.Controllers
{
    public class AdminOrderController : AdminBaseController
    {
        private readonly AppDbContext _context;
        private readonly IGhnService _ghnService;

        public AdminOrderController(AppDbContext context, IGhnService ghnService)
        {
            _context = context;
            _ghnService = ghnService;
        }

        // GET: AdminOrder
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: AdminOrder/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(o => o.Payments)
                .Include(o => o.Shippings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: AdminOrder/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            string oldStatus = order.Status ?? "Chờ xác nhận";
            order.Status = status;
            
            // Update shipping status
            var shipping = await _context.Shippings.FirstOrDefaultAsync(s => s.OrderId == id);
            if (shipping == null)
            {
                var orderUser = await _context.Users.FindAsync(order.UserId);
                shipping = new Shipping 
                { 
                    OrderId = id,
                    Address = orderUser?.Address ?? "Chưa có địa chỉ",
                    Phone = orderUser?.Phone ?? ""
                };
                _context.Shippings.Add(shipping);
            }

            if (status == "Đang giao")
            {
                shipping.ShippingStatus = "Đang giao hàng";
                shipping.ShippingDate = DateTime.Now;
                if (string.IsNullOrEmpty(shipping.Carrier))
                {
                    shipping.Carrier = "MeiLing Express";
                    shipping.TrackingCode = $"ML{DateTime.Now:yyyyMMdd}{order.Id}";
                }
                if (shipping.EstimatedDeliveryDate == null)
                {
                    shipping.EstimatedDeliveryDate = DateTime.Now.AddDays(4);
                }
            }
            else if (status == "Đã giao")
            {
                shipping.ShippingStatus = "Giao hàng thành công";
                shipping.ShippingDate = DateTime.Now;
            }
            else if (status == "Hoàn thành")
            {
                shipping.ShippingStatus = "Đã nhận hàng";
            }
            else if (status == "Giao thất bại")
            {
                shipping.ShippingStatus = "Giao hàng thất bại";
            }
            else if (status == "Hoàn hàng")
            {
                shipping.ShippingStatus = "Đang hoàn hàng về kho";
            }
            else if (status == "Hủy" || status == "Đã hủy" || status == "Cancelled")
            {
                shipping.ShippingStatus = "Đã hủy đơn";
                await Services.WarehouseHelper.RestoreOrderStockAsync(_context, id);
            }

            // Award loyalty points only once when changing to "Đã giao" or "Hoàn thành"
            if ((status == "Đã giao" || status == "Hoàn thành") && 
                oldStatus != "Đã giao" && oldStatus != "Hoàn thành")
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user != null)
                {
                    int pointsEarned = (int)Math.Floor((order.TotalAmount ?? 0) / 10000);
                    if (pointsEarned > 0)
                    {
                        user.Points += pointsEarned;
                        TempData["SuccessMessage"] = $"Cập nhật trạng thái thành công! Khách hàng đã được cộng +{pointsEarned} điểm tích lũy.";
                    }
                }
            }

            // Update payment status
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == id);
            if (payment != null)
            {
                if (status == "Đã giao" || status == "Hoàn thành")
                {
                    payment.PaymentStatus = "Đã thanh toán";
                    payment.PaymentDate = DateTime.Now;
                }
                else if (status == "Hủy" || status == "Đã hủy" || status == "Cancelled")
                {
                    payment.PaymentStatus = "Đã hủy";
                }
            }

            await _context.SaveChangesAsync();

            if (TempData["SuccessMessage"] == null)
            {
                TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng thành công sang '{status}'!";
            }
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // POST: AdminOrder/HandoverToCarrier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandoverToCarrier(int id, string carrier)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = "Đang giao";

            var shipping = await _context.Shippings.FirstOrDefaultAsync(s => s.OrderId == id);
            if (shipping == null)
            {
                shipping = new Shipping { OrderId = id };
                _context.Shippings.Add(shipping);
            }

            shipping.Carrier = carrier;
            string prefix = carrier == "GHN" ? "GHN" : "GHTK";
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            string seq = order.Id.ToString().PadLeft(4, '0');
            shipping.TrackingCode = $"{prefix}{dateStr}{seq}";
            shipping.ShippingStatus = "Đang giao";
            shipping.ShippingDate = DateTime.Now;
            shipping.EstimatedDeliveryDate = DateTime.Now.AddDays(4);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đơn hàng đã được bàn giao cho {carrier}. Mã vận đơn tự sinh: {shipping.TrackingCode}";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // POST: AdminOrder/CreateGhnOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGhnOrder(int id, int districtId = 1442, string wardCode = "20101")
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Shippings)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var shipping = order.Shippings.FirstOrDefault();
            string receiverName = order.User?.FullName ?? "Khách hàng";
            string receiverPhone = shipping?.Phone ?? order.User?.Phone ?? "0900000000";
            string address = shipping?.Address ?? order.User?.Address ?? "123 Đường chính";
            
            bool isCod = order.Payments.Any(p => p.PaymentMethod == "COD" && (p.PaymentStatus ?? "").ToLower() != "completed");
            decimal codAmount = isCod ? (order.TotalAmount ?? 0m) : 0m;

            var result = await _ghnService.CreateOrderAsync(order.Id, receiverName, receiverPhone, address, wardCode, districtId, codAmount);

            if (result.Success)
            {
                if (shipping == null)
                {
                    shipping = new Shipping { OrderId = id };
                    _context.Shippings.Add(shipping);
                }

                shipping.Carrier = "Giao Hàng Nhanh (GHN)";
                shipping.TrackingCode = result.OrderCode;
                shipping.ShippingStatus = "Đang giao";
                shipping.ShippingDate = DateTime.Now;
                shipping.EstimatedDeliveryDate = DateTime.Now.AddDays(3);

                order.Status = "Đang giao";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tạo vận đơn GHN thành công! Mã vận đơn GHN: {result.OrderCode}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Lỗi khi tạo vận đơn GHN: {result.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
