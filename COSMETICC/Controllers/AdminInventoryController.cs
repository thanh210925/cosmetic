using System;
using System.Linq;
using System.Threading.Tasks;
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

            // Near expiry alerts across all batches (expiring within 60 days)
            var now = DateTime.Now;
            var alertDate = now.AddDays(60);
            
            var expiringBatchesCount = await _context.ProductBatches
                .CountAsync(b => b.Quantity > 0 && b.ExpiryDate <= alertDate);

            ViewBag.ExpiringBatchesCount = expiringBatchesCount;

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

        // GET: AdminInventory/Batches
        public async Task<IActionResult> Batches()
        {
            var now = DateTime.Now;
            var warningDate = now.AddDays(60); // warning threshold: 60 days

            var batches = await _context.ProductBatches
                .Include(b => b.Product)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();

            ViewBag.Now = now;
            ViewBag.WarningDate = warningDate;

            // Load list of products for the import dropdown
            ViewBag.ProductsList = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(batches);
        }

        // POST: AdminInventory/AddBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBatch(int productId, string batchNumber, int importQuantity, DateTime expiryDate, string? notes)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại!";
                return RedirectToAction(nameof(Batches));
            }

            if (importQuantity <= 0)
            {
                TempData["ErrorMessage"] = "Số lượng nhập lô phải lớn hơn 0!";
                return RedirectToAction(nameof(Batches));
            }

            if (expiryDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Hạn sử dụng của lô hàng phải lớn hơn ngày hiện tại!";
                return RedirectToAction(nameof(Batches));
            }

            // Create new batch
            var batch = new ProductBatch
            {
                ProductId = productId,
                BatchNumber = batchNumber.Trim().ToUpper(),
                ImportQuantity = importQuantity,
                Quantity = importQuantity,
                ImportDate = DateTime.Now,
                ExpiryDate = expiryDate,
                Notes = notes
            };

            _context.ProductBatches.Add(batch);

            // Update product total stock
            product.Stock = (product.Stock ?? 0) + importQuantity;
            _context.Products.Update(product);

            // Write inventory log
            var log = new InventoryLog
            {
                ProductId = productId,
                Type = "NHAP",
                Quantity = importQuantity,
                Note = $"Nhập lô hàng mới: {batch.BatchNumber} | HSD: {expiryDate:dd/MM/yyyy}. {notes}",
                CreatedAt = DateTime.Now
            };
            _context.InventoryLogs.Add(log);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Nhập lô hàng {batch.BatchNumber} thành công cho sản phẩm {product.Name}!";
            return RedirectToAction(nameof(Batches));
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

                // Deduct from oldest active batches first (FIFO)
                int quantityToDeduct = quantity;
                var activeBatches = await _context.ProductBatches
                    .Where(b => b.ProductId == productId && b.Quantity > 0)
                    .OrderBy(b => b.ExpiryDate) // oldest expiry date first
                    .ToListAsync();

                foreach (var batch in activeBatches)
                {
                    if (quantityToDeduct <= 0) break;

                    if (batch.Quantity >= quantityToDeduct)
                    {
                        batch.Quantity -= quantityToDeduct;
                        quantityToDeduct = 0;
                    }
                    else
                    {
                        quantityToDeduct -= batch.Quantity;
                        batch.Quantity = 0;
                    }
                    _context.ProductBatches.Update(batch);
                }
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

        // ================= BARCODE / QR SCAN WORKFLOW =================

        // GET: /AdminInventory/GetProductByBarcode
        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                return Json(new { success = false, message = "Vui lòng nhập hoặc quét mã Barcode/SKU!" });
            }

            var cleanBarcode = barcode.Trim();

            // Search product by Barcode or SKU
            var product = await _context.Products
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Barcode == cleanBarcode || p.SKU == cleanBarcode);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm nào khớp với mã Barcode/SKU này!" });
            }

            // Get product batches
            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == product.Id && b.Quantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    b.Quantity,
                    ExpiryDate = b.ExpiryDate.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                product = new
                {
                    product.Id,
                    product.Name,
                    BrandName = product.Brand != null ? product.Brand.Name : "Không rõ",
                    product.Stock,
                    product.SKU,
                    product.Barcode,
                    ImageUrl = product.ImageUrl ?? ""
                },
                batches
            });
        }

        // POST: /AdminInventory/AuditStock
        [HttpPost]
        public async Task<IActionResult> AuditStock(int productId, int actualQty, string note)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            int systemQty = product.Stock ?? 0;
            int diff = actualQty - systemQty;

            if (diff == 0)
            {
                return Json(new { success = true, message = "Số lượng thực tế khớp hoàn hảo với hệ thống! Không cần điều chỉnh." });
            }

            product.Stock = actualQty;
            _context.Products.Update(product);

            // Record as AUDIT log
            var log = new InventoryLog
            {
                ProductId = productId,
                Type = "KIEMKE",
                Quantity = Math.Abs(diff),
                Note = $"Kiểm kê kho bằng QR/Barcode. SL hệ thống: {systemQty} | SL thực tế: {actualQty} | Chênh lệch: {(diff > 0 ? "+" : "")}{diff}. Ghi chú: {note}",
                CreatedAt = DateTime.Now
            };
            _context.InventoryLogs.Add(log);

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Đã điều chỉnh kho thành công! Chênh lệch: {(diff > 0 ? "+" : "")}{diff}. Lượng tồn mới: {actualQty}." 
            });
        }
    }
}
