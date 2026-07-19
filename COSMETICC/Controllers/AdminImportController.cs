using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;

namespace COSMETICC.Controllers
{
    public class AdminImportController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminImportController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminImport
        public async Task<IActionResult> Index()
        {
            var receipts = await _context.ImportReceipts
                .Include(r => r.Supplier)
                .Include(r => r.Admin)
                .OrderByDescending(r => r.ImportDate)
                .ToListAsync();

            return View(receipts);
        }

        // GET: AdminImport/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var receipt = await _context.ImportReceipts
                .Include(r => r.Supplier)
                .Include(r => r.Admin)
                .Include(r => r.ImportReceiptDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (receipt == null) return NotFound();

            return View(receipt);
        }

        // GET: AdminImport/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            
            // Auto generate receipt code
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            int todayCount = await _context.ImportReceipts
                .CountAsync(r => r.ReceiptCode.StartsWith("PN-" + dateStr));
            ViewBag.NextReceiptCode = $"PN-{dateStr}-{(todayCount + 1):D4}";

            return View();
        }

        // POST: AdminImport/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ImportReceipt model, List<ImportReceiptDetail> details)
        {
            if (details == null || !details.Any())
            {
                ModelState.AddModelError("", "Phiếu nhập phải có ít nhất một mặt hàng.");
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var adminIdStr = HttpContext.Session.GetString("AdminId");
                    int? adminId = null;
                    if (int.TryParse(adminIdStr, out int aId)) adminId = aId;

                    // Calculate totals
                    decimal subTotal = details.Sum(d => d.Quantity * d.UnitPrice);
                    decimal total = subTotal - model.Discount + model.ShippingFee + model.Tax;

                    model.AdminId = adminId;
                    model.SubTotal = subTotal;
                    model.TotalAmount = total;
                    model.Status = "Draft"; // Luôn bắt đầu ở dạng Nháp
                    model.CreatedAt = DateTime.Now;

                    _context.ImportReceipts.Add(model);
                    await _context.SaveChangesAsync();

                    foreach (var detail in details)
                    {
                        detail.ReceiptId = model.Id;
                        _context.ImportReceiptDetails.Add(detail);
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = $"Tạo phiếu nhập nháp {model.ReceiptCode} thành công! Hãy xác nhận phiếu để nhập kho.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu phiếu nhập: " + ex.Message);
                }
            }

            ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            return View(model);
        }

        // POST: AdminImport/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var receipt = await _context.ImportReceipts
                .Include(r => r.ImportReceiptDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();
            if (receipt.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận phiếu nhập ở trạng thái Nháp (Draft).";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                receipt.Status = "Confirmed";
                _context.ImportReceipts.Update(receipt);

                foreach (var item in receipt.ImportReceiptDetails)
                {
                    // 1. Update Product Total Stock
                    var product = item.Product;
                    product.Stock = (product.Stock ?? 0) + item.Quantity;
                    _context.Products.Update(product);

                    // 2. Create ProductBatch
                    var batch = new ProductBatch
                    {
                        ProductId = item.ProductId,
                        ImportReceiptDetailId = item.Id,
                        BatchNumber = item.BatchNumber.Trim().ToUpper(),
                        ImportQuantity = item.Quantity,
                        Quantity = item.Quantity,
                        RemainingQuantity = item.Quantity,
                        ManufactureDate = item.ManufactureDate,
                        ExpiryDate = item.ExpiryDate,
                        ImportDate = DateTime.Now,
                        Notes = $"Nhập kho qua phiếu {receipt.ReceiptCode}"
                    };
                    _context.ProductBatches.Add(batch);

                    // 3. Write InventoryLog
                    var log = new InventoryLog
                    {
                        ProductId = item.ProductId,
                        Type = "IMPORT",
                        Quantity = item.Quantity,
                        BatchNumber = batch.BatchNumber,
                        ReferenceCode = receipt.ReceiptCode,
                        Note = $"Nhập kho phiếu {receipt.ReceiptCode} | HSD: {item.ExpiryDate:dd/MM/yyyy}",
                        CreatedAt = DateTime.Now
                    };
                    _context.InventoryLogs.Add(log);
                }

                await _context.SaveChangesAsync();

                // 4. Update Product Expiry Dates based on new active batches
                foreach (var item in receipt.ImportReceiptDetails)
                {
                    await Services.WarehouseHelper.UpdateProductExpiryDateAsync(_context, item.ProductId);
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Xác nhận và nhập kho thành công phiếu {receipt.ReceiptCode}!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xác nhận phiếu nhập: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: AdminImport/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var receipt = await _context.ImportReceipts
                .Include(r => r.ImportReceiptDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();
            
            if (receipt.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "Phiếu nhập này đã được hủy từ trước.";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (receipt.Status == "Confirmed")
                {
                    // Kiểm tra xem có lô hàng nào thuộc phiếu nhập này đã bị xuất bán (RemainingQuantity < ImportQuantity) chưa
                    var detailIds = receipt.ImportReceiptDetails.Select(d => (int?)d.Id).ToList();
                    
                    var batches = await _context.ProductBatches
                        .Where(b => detailIds.Contains(b.ImportReceiptDetailId))
                        .ToListAsync();

                    foreach (var batch in batches)
                    {
                        if (batch.RemainingQuantity < batch.ImportQuantity)
                        {
                            TempData["ErrorMessage"] = $"Không thể hủy phiếu nhập do lô hàng {batch.BatchNumber} của sản phẩm {batch.Product?.Name} đã được bán ra một phần.";
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }

                    // Nếu chưa bán mặt hàng nào, thực hiện giảm tồn kho
                    foreach (var item in receipt.ImportReceiptDetails)
                    {
                        var product = item.Product;
                        product.Stock = Math.Max(0, (product.Stock ?? 0) - item.Quantity);
                        _context.Products.Update(product);

                        // Xóa các batches tương ứng
                        var associatedBatches = batches.Where(b => b.ImportReceiptDetailId == item.Id).ToList();
                        foreach (var b in associatedBatches)
                        {
                            _context.ProductBatches.Remove(b);
                        }

                        // Ghi nhận log hủy nhập
                        var log = new InventoryLog
                        {
                            ProductId = item.ProductId,
                            Type = "ADJUST", // Điều chỉnh kho
                            Quantity = item.Quantity,
                            BatchNumber = item.BatchNumber.ToUpper(),
                            ReferenceCode = receipt.ReceiptCode,
                            Note = $"Hủy phiếu nhập kho {receipt.ReceiptCode} — Khấu trừ hoàn trả kho",
                            CreatedAt = DateTime.Now
                        };
                        _context.InventoryLogs.Add(log);
                    }
                }

                receipt.Status = "Cancelled";
                _context.ImportReceipts.Update(receipt);

                await _context.SaveChangesAsync();

                // Update product expiry dates after batches removal
                if (receipt.Status == "Cancelled")
                {
                    foreach (var item in receipt.ImportReceiptDetails)
                    {
                        await Services.WarehouseHelper.UpdateProductExpiryDateAsync(_context, item.ProductId);
                    }
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Hủy phiếu nhập {receipt.ReceiptCode} và khôi phục tồn kho thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy phiếu nhập: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
