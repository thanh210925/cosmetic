using COSMETICC.Libraries;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
// using COSMETICC.Libraries; // Mở comment này nếu bạn để VnPayLibrary ở thư mục Libraries

namespace COSMETICC.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Action xử lý tạo đơn hàng và chuyển hướng sang VNPay
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePayment()
        {
            // 1. Kiểm tra đăng nhập
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thanh toán!";
                return RedirectToAction("Login", "Account");
            }

            // 2. Lấy giỏ hàng của User kèm theo các mặt hàng và thông tin sản phẩm
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product) // Cần Include Product để lấy giá tiền
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            // 3. Tính tổng tiền từ giỏ hàng (Giả định Product có thuộc tính Price)
            // Nếu thuộc tính giá của bạn tên khác (VD: SellPrice), hãy đổi lại cho khớp nhé
            decimal totalAmount = cart.CartItems.Sum(ci => ci.Quantity * (ci.Product.PromoPrice ?? ci.Product.Price)) ?? 0m;

            // 4. Tạo Order mới
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Pending"
            };

            // 5. Chuyển CartItem sang OrderDetail
            // Vì bạn đã cấu hình List<OrderDetail> trong model Order, ta có thể add trực tiếp
            foreach (var item in cart.CartItems)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    // Price = item.Product.Price // Thêm dòng này nếu OrderDetail của bạn có lưu lại giá tại thời điểm mua
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu Order và OrderDetails vào DB

            // 6. Tạo Payment
            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = "VNPay",
                PaymentStatus = "Pending",
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // 7. Xây dựng URL chuyển hướng VNPay
            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", _configuration["VnPay:Version"]);
            vnpay.AddRequestData("vnp_Command", _configuration["VnPay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]);

            // VNPay yêu cầu số tiền nhân 100
            long amount = (long)(order.TotalAmount * 100);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());

            vnpay.AddRequestData("vnp_CreateDate", order.OrderDate.Value.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _configuration["VnPay:CurrCode"]);

            // Lấy IP của Client
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);

            vnpay.AddRequestData("vnp_Locale", _configuration["VnPay:Locale"]);
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _configuration["VnPay:ReturnUrl"]);
            string tick = DateTime.Now.Ticks.ToString();
            vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString() + "_" + tick);

            string paymentUrl = vnpay.CreateRequestUrl(_configuration["VnPay:BaseUrl"], _configuration["VnPay:HashSecret"]);

            // 8. Chuyển hướng người dùng sang VNPay
            return Redirect(paymentUrl);
        }

        /// <summary>
        /// Action nhận Callback từ VNPay trả về sau khi thanh toán xong
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var vnpayData = Request.Query;
            VnPayLibrary vnpay = new VnPayLibrary();

            foreach (var (key, value) in vnpayData)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            // Lấy chuỗi mã giao dịch về
            string txnRef = vnpay.GetResponseData("vnp_TxnRef");
            // Cắt theo dấu "_" và chỉ lấy mảng đầu tiên [0] chính là Order.Id
            int orderId = Convert.ToInt32(txnRef.Split('_')[0]);
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = Request.Query["vnp_SecureHash"];

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _configuration["VnPay:HashSecret"]);

            if (checkSignature)
            {
                var order = await _context.Orders.FindAsync(orderId);
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

                if (vnp_ResponseCode == "00") // "00" = Giao dịch thành công
                {
                    if (order != null)
                    {
                        order.Status = "Completed";

                        // [TODO 1]: Trừ số lượng (Stock) trong bảng Products
                        // Giả định bạn có bảng OrderDetails chứa các mặt hàng khách đã mua
                        var orderDetails = await _context.OrderDetails.Where(od => od.OrderId == orderId).ToListAsync();
                        foreach (var item in orderDetails)
                        {
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product != null && product.Stock >= item.Quantity)
                            {
                                product.Stock -= item.Quantity;
                            }
                        }

                        // [TODO 2]: Xóa dữ liệu bảng CartItems tương ứng với user
                        var cart = await _context.Carts
                            .Include(c => c.CartItems)
                            .FirstOrDefaultAsync(c => c.UserId == order.UserId);

                        if (cart != null && cart.CartItems.Any())
                        {
                            _context.CartItems.RemoveRange(cart.CartItems);
                        }
                    }

                    if (payment != null)
                    {
                        payment.PaymentStatus = "Success";
                        payment.PaymentDate = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    return View("PaymentSuccess");
                }
                else
                {
                    // Giao dịch thất bại / bị hủy
                    if (order != null) order.Status = "Failed";
                    if (payment != null) payment.PaymentStatus = "Failed";

                    await _context.SaveChangesAsync();
                    return View("PaymentFailed");
                }
            }
            else
            {
                // Chữ ký không hợp lệ
                return View("PaymentError");
            }
        }
        
    }
}