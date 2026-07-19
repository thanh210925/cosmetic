using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminOrderController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminOrderController(AppDbContext context)
        {
            _context = context;
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
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            
            // Update shipping status
            var shipping = await _context.Shippings.FirstOrDefaultAsync(s => s.OrderId == id);
            if (shipping != null)
            {
                if (status == "Đang giao")
                {
                    shipping.ShippingStatus = "Đang giao hàng";
                    shipping.ShippingDate = DateTime.Now;
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
                else if (status == "Hủy")
                {
                    shipping.ShippingStatus = "Đã hủy đơn";
                }
                _context.Update(shipping);
            }

            // Update payment status
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == id);
            if (payment != null)
            {
                if (status == "Hoàn thành")
                {
                    payment.PaymentStatus = "Đã thanh toán";
                    payment.PaymentDate = DateTime.Now;
                }
                else if (status == "Hủy")
                {
                    payment.PaymentStatus = "Đã hủy";
                }
                _context.Update(payment);
            }

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng thành công sang '{status}'!";
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
