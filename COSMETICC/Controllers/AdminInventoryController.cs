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
            // Redirect to AdminImport/Create to enforce Import Receipts workflow
            TempData["ErrorMessage"] = "Vui lòng lập Phiếu Nhập Kho để nhập thêm lô hàng mới!";
            return RedirectToAction("Create", "AdminImport");
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

            string finalType = type;
            if (type == "NHAP")
            {
                product.Stock = (product.Stock ?? 0) + quantity;
                finalType = "IMPORT";

                // Log without batch details since it's direct adjust
                var directLog = new InventoryLog
                {
                    ProductId = productId,
                    Type = "IMPORT",
                    Quantity = quantity,
                    Note = $"Điều chỉnh tăng kho trực tiếp: {note}",
                    CreatedAt = DateTime.Now
                };
                _context.InventoryLogs.Add(directLog);
            }
            else if (type == "XUAT")
            {
                if ((product.Stock ?? 0) < quantity)
                {
                    TempData["ErrorMessage"] = "Số lượng xuất kho vượt quá lượng tồn hiện tại!";
                    return RedirectToAction(nameof(Index));
                }
                product.Stock = (product.Stock ?? 0) - quantity;
                finalType = "ADJUST";

                // Deduct from oldest active batches first (FIFO) using RemainingQuantity
                int quantityToDeduct = quantity;
                var activeBatches = await _context.ProductBatches
                    .Where(b => b.ProductId == productId && b.RemainingQuantity > 0)
                    .OrderBy(b => b.ExpiryDate) // oldest expiry date first
                    .ToListAsync();

                foreach (var batch in activeBatches)
                {
                    if (quantityToDeduct <= 0) break;

                    int qtyDeducted = 0;
                    if (batch.RemainingQuantity >= quantityToDeduct)
                    {
                        qtyDeducted = quantityToDeduct;
                        batch.RemainingQuantity -= quantityToDeduct;
                        quantityToDeduct = 0;
                    }
                    else
                    {
                        qtyDeducted = batch.RemainingQuantity;
                        quantityToDeduct -= batch.RemainingQuantity;
                        batch.RemainingQuantity = 0;
                    }
                    _context.ProductBatches.Update(batch);

                    // Log audit details per batch
                    var batchLog = new InventoryLog
                    {
                        ProductId = productId,
                        Type = "ADJUST",
                        Quantity = qtyDeducted,
                        BatchNumber = batch.BatchNumber,
                        Note = $"Điều chỉnh giảm kho trực tiếp (Lô: {batch.BatchNumber}). Lý do: {note}",
                        CreatedAt = DateTime.Now
                    };
                    _context.InventoryLogs.Add(batchLog);
                }
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            // Tự động tính toán lại HSD từ lô hàng còn tồn gần nhất
            await Services.WarehouseHelper.UpdateProductExpiryDateAsync(_context, productId);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Điều chỉnh kho thành công cho sản phẩm {product.Name}!";
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

            // Get product batches using RemainingQuantity
            var batches = await _context.ProductBatches
                .Where(b => b.ProductId == product.Id && b.RemainingQuantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    Quantity = b.RemainingQuantity,
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

            // If quantity decreases, we deduct using FIFO
            if (diff < 0)
            {
                int qtyToDeduct = Math.Abs(diff);
                var activeBatches = await _context.ProductBatches
                    .Where(b => b.ProductId == productId && b.RemainingQuantity > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .ToListAsync();

                foreach (var batch in activeBatches)
                {
                    if (qtyToDeduct <= 0) break;

                    int qtyDeducted = 0;
                    if (batch.RemainingQuantity >= qtyToDeduct)
                    {
                        qtyDeducted = qtyToDeduct;
                        batch.RemainingQuantity -= qtyToDeduct;
                        qtyToDeduct = 0;
                    }
                    else
                    {
                        qtyDeducted = batch.RemainingQuantity;
                        qtyToDeduct -= batch.RemainingQuantity;
                        batch.RemainingQuantity = 0;
                    }
                    _context.ProductBatches.Update(batch);

                    var batchLog = new InventoryLog
                    {
                        ProductId = productId,
                        Type = "AUDIT",
                        Quantity = qtyDeducted,
                        BatchNumber = batch.BatchNumber,
                        Note = $"Kiểm kê chênh lệch âm (Lô: {batch.BatchNumber}). Chi tiết: {note}",
                        CreatedAt = DateTime.Now
                    };
                    _context.InventoryLogs.Add(batchLog);
                }
            }
            else // diff > 0, we can't easily assign new batch info directly, we log to general direct import
            {
                var auditLog = new InventoryLog
                {
                    ProductId = productId,
                    Type = "AUDIT",
                    Quantity = diff,
                    Note = $"Kiểm kê chênh lệch dương. SL hệ thống: {systemQty} | SL thực tế: {actualQty}. Ghi chú: {note}",
                    CreatedAt = DateTime.Now
                };
                _context.InventoryLogs.Add(auditLog);
            }

            product.Stock = actualQty;
            _context.Products.Update(product);

            await _context.SaveChangesAsync();

            // Tự động tính toán lại HSD từ lô hàng còn tồn gần nhất
            await Services.WarehouseHelper.UpdateProductExpiryDateAsync(_context, productId);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Đã điều chỉnh kho thành công! Chênh lệch: {(diff > 0 ? "+" : "")}{diff}. Lượng tồn mới: {actualQty}." 
            });
        }
    }
}
