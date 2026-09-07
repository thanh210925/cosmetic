using System;
using System.Linq;
using System.Threading.Tasks;
using COSMETICC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Controllers
{
    public class AdminReturnController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminReturnController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var request = await _context.ReturnRequests
                .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Approved";
            request.AdminNote = adminNote;
            request.ProcessedAt = DateTime.Now;

            // Restock products & update order status
            if (request.Order != null)
            {
                request.Order.Status = "Returned";
                foreach (var detail in request.Order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.Stock = (product.Stock ?? 0) + (detail.Quantity ?? 1);

                        // Add inventory log
                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = product.Id,
                            Type = "NHAP",
                            Quantity = detail.Quantity ?? 1,
                            Note = $"Khách trả hàng - Đơn #{request.Order.OrderCode ?? request.Order.Id.ToString()}",
                            CreatedAt = DateTime.Now
                        });
                    }


                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã duyệt yêu cầu trả hàng cho đơn #{request.Order?.OrderCode ?? request.OrderId.ToString()}. Trạng thái kho đã được hoàn.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string? adminNote)
        {
            var request = await _context.ReturnRequests.FindAsync(id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Rejected";
            request.AdminNote = adminNote;
            request.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã từ chối yêu cầu trả hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
