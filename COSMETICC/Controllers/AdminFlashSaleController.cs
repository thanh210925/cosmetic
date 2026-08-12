using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminFlashSaleController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminFlashSaleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /AdminFlashSale
        public async Task<IActionResult> Index(string? status)
        {
            var now = DateTime.Now;

            // Auto update expired campaigns
            var expired = await _context.FlashSales.Where(fs => fs.IsActive && fs.EndTime <= now).ToListAsync();
            if (expired.Any())
            {
                foreach (var e in expired) e.IsActive = false;
                await _context.SaveChangesAsync();
            }

            var campaigns = await _context.FlashSales
                .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fsi => fsi.Product)
                        .ThenInclude(p => p.Category)
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync();

            var activeCampaign = campaigns.FirstOrDefault(fs => fs.IsActive && fs.StartTime <= now && fs.EndTime > now)
                                 ?? campaigns.FirstOrDefault(fs => fs.IsActive);

            var activeItems = activeCampaign?.FlashSaleItems?.Where(i => i.IsActive).ToList() ?? new List<FlashSaleItem>();

            var allProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Id)
                .ToListAsync();

            ViewBag.ActiveCampaign = activeCampaign;
            ViewBag.ActiveProductCount = activeItems.Count;
            ViewBag.TotalProductCount = allProducts.Count;
            ViewBag.AllProducts = allProducts;
            ViewBag.FlashSaleItemsMap = activeCampaign?.FlashSaleItems?.ToDictionary(i => i.ProductId) ?? new Dictionary<int, FlashSaleItem>();

            return View(campaigns);
        }

        // GET: /AdminFlashSale/Create
        public async Task<IActionResult> Create()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.ImageUrl,
                    p.Stock
                })
                .ToListAsync();

            ViewBag.Products = products;
            return View();
        }

        // POST: /AdminFlashSale/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlashSale model, List<int> productIds, List<decimal> discountPrices, List<int> quantityForSales)
        {
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu!");
            }

            if (productIds == null || !productIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một sản phẩm tham gia Flash Sale!");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            _context.FlashSales.Add(model);
            await _context.SaveChangesAsync();

            for (int i = 0; i < productIds.Count; i++)
            {
                var pid = productIds[i];
                var price = (i < discountPrices.Count) ? discountPrices[i] : 0;
                var qty = (i < quantityForSales.Count) ? quantityForSales[i] : 1;

                if (price <= 0 || qty <= 0) continue;

                var item = new FlashSaleItem
                {
                    FlashSaleId = model.Id,
                    ProductId = pid,
                    DiscountPrice = price,
                    QuantityForSale = qty,
                    SoldQuantity = 0,
                    IsActive = true
                };

                _context.FlashSaleItems.Add(item);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tạo chương trình Flash Sale thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminFlashSale/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var campaign = await _context.FlashSales
                .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fsi => fsi.Product)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (campaign == null)
            {
                return NotFound();
            }

            var existingProductIds = campaign.FlashSaleItems.Select(i => i.ProductId).ToList();

            var availableProducts = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.AvailableProducts = availableProducts;
            return View(campaign);
        }

        // POST: /AdminFlashSale/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlashSale model, List<int> productIds, List<decimal> discountPrices, List<int> quantityForSales, List<int> soldQuantities)
        {
            if (id != model.Id) return NotFound();

            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu!");
            }

            var campaign = await _context.FlashSales
                .Include(fs => fs.FlashSaleItems)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (campaign == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.AvailableProducts = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
                return View(model);
            }

            campaign.Title = model.Title;
            campaign.StartTime = model.StartTime;
            campaign.EndTime = model.EndTime;
            campaign.IsActive = model.IsActive;

            // Remove items not in submitted list
            var itemsToRemove = campaign.FlashSaleItems.Where(i => !productIds.Contains(i.ProductId)).ToList();
            _context.FlashSaleItems.RemoveRange(itemsToRemove);

            // Update or Add items
            for (int i = 0; i < productIds.Count; i++)
            {
                var pid = productIds[i];
                var price = (i < discountPrices.Count) ? discountPrices[i] : 0;
                var qty = (i < quantityForSales.Count) ? quantityForSales[i] : 1;
                var sold = (i < soldQuantities.Count) ? soldQuantities[i] : 0;

                var existingItem = campaign.FlashSaleItems.FirstOrDefault(item => item.ProductId == pid);
                if (existingItem != null)
                {
                    existingItem.DiscountPrice = price;
                    existingItem.QuantityForSale = qty;
                    existingItem.SoldQuantity = sold;
                    existingItem.IsActive = sold < qty && campaign.IsActive;
                }
                else
                {
                    var newItem = new FlashSaleItem
                    {
                        FlashSaleId = campaign.Id,
                        ProductId = pid,
                        DiscountPrice = price,
                        QuantityForSale = qty,
                        SoldQuantity = sold,
                        IsActive = sold < qty
                    };
                    _context.FlashSaleItems.Add(newItem);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật chương trình Flash Sale thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminFlashSale/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var campaign = await _context.FlashSales
                .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fsi => fsi.Product)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (campaign == null) return NotFound();

            return View(campaign);
        }

        // POST: /AdminFlashSale/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var campaign = await _context.FlashSales.FindAsync(id);
            if (campaign == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chiến dịch!" });
            }

            campaign.IsActive = !campaign.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = campaign.IsActive, message = "Cập nhật trạng thái thành công!" });
        }

        // POST: /AdminFlashSale/ToggleItemStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleItemStatus(int id)
        {
            var item = await _context.FlashSaleItems.FindAsync(id);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm Flash Sale!" });
            }

            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = item.IsActive, message = "Đã cập nhật trạng thái sản phẩm Flash Sale!" });
        }

        // POST: /AdminFlashSale/QuickSaveItem
        [HttpPost]
        public async Task<IActionResult> QuickSaveItem(int productId, decimal discountPrice, int quantityForSale)
        {
            var now = DateTime.Now;
            var activeFs = await _context.FlashSales.FirstOrDefaultAsync(fs => fs.IsActive && fs.EndTime > now);
            if (activeFs == null)
            {
                activeFs = new FlashSale
                {
                    Title = "🔥 FLASH SALE GIỜ VÀNG MỚI",
                    StartTime = now.AddHours(-1),
                    EndTime = now.AddDays(7),
                    IsActive = true,
                    CreatedAt = now
                };
                _context.FlashSales.Add(activeFs);
                await _context.SaveChangesAsync();
            }

            var item = await _context.FlashSaleItems.FirstOrDefaultAsync(i => i.FlashSaleId == activeFs.Id && i.ProductId == productId);
            if (item != null)
            {
                item.DiscountPrice = discountPrice;
                item.QuantityForSale = quantityForSale;
                item.IsActive = true;
            }
            else
            {
                item = new FlashSaleItem
                {
                    FlashSaleId = activeFs.Id,
                    ProductId = productId,
                    DiscountPrice = discountPrice,
                    QuantityForSale = quantityForSale,
                    SoldQuantity = 0,
                    IsActive = true
                };
                _context.FlashSaleItems.Add(item);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lưu cài đặt Flash Sale thành công!" });
        }

        // POST: /AdminFlashSale/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var campaign = await _context.FlashSales
                .Include(fs => fs.FlashSaleItems)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (campaign != null)
            {
                _context.FlashSaleItems.RemoveRange(campaign.FlashSaleItems);
                _context.FlashSales.Remove(campaign);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa chương trình Flash Sale thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
