using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminInventoryController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminInventoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminInventory
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            return View(products);
        }

        // GET: AdminInventory/Logs
        public async Task<IActionResult> Logs()
        {
            var logs = await _context.InventoryLogs
                .Include(l => l.Product)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(logs);
        }

        // POST: AdminInventory/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(int productId, string type, int quantity, string note)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Số lượng thay đổi phải lớn hơn 0!";
                return RedirectToAction(nameof(Index));
            }

            if (type == "NHAP")
            {
                product.Stock = (product.Stock ?? 0) + quantity;
            }
            else if (type == "XUAT")
            {
                if ((product.Stock ?? 0) < quantity)
                {
                    TempData["ErrorMessage"] = "Số lượng xuất kho vượt quá lượng tồn hiện tại!";
                    return RedirectToAction(nameof(Index));
                }
                product.Stock = (product.Stock ?? 0) - quantity;
            }

            var log = new InventoryLog
            {
                ProductId = productId,
                Type = type,
                Quantity = quantity,
                Note = note,
                CreatedAt = DateTime.Now
            };

            _context.InventoryLogs.Add(log);
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{(type == "NHAP" ? "Nhập" : "Xuất")} kho thành công cho sản phẩm {product.Name}!";
            return RedirectToAction(nameof(Index));
        }
    }
}
