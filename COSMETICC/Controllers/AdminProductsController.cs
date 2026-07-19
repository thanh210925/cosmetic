namespace COSMETICC.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

public class AdminProductsController : AdminBaseController
{
    private readonly AppDbContext _context;

    public AdminProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: PRODUCTS
    public async Task<IActionResult> Index()    
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .ToListAsync();
        return View(products);
    }

    // GET: PRODUCTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // GET: PRODUCTS/Create
    public IActionResult Create()
    {
        ViewBag.Categories = _context.Categories.ToList();
        ViewBag.Brands = _context.Brands.ToList();
        return View();
    }

    // POST: PRODUCTS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Price,PromoPrice,Description,Ingredient,ImageUrl,CategoryId,BrandId,Stock,IsVegan,ExpiryDate,IsActive,SkinType,SKU,Barcode,Slug,VideoUrl,Tags,MetaTitle,MetaDescription,MetaKeywords")] Product product)
    {
        if (ModelState.IsValid)
        {
            product.CreatedAt = DateTime.Now;
            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Categories = _context.Categories.ToList();
        ViewBag.Brands = _context.Brands.ToList();
        return View(product);
    }
    [HttpPost]
    public async Task<IActionResult> CreateMultiple(List<Product> products)
    {
        foreach (var product in products)
        {
            product.CreatedAt = DateTime.Now;

            _context.Products.Add(product);
        }
        ViewBag.Categories = _context.Categories.ToList();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: PRODUCTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        ViewBag.Categories = _context.Categories.ToList();
        ViewBag.Brands = _context.Brands.ToList();
        return View(product);
    }

    // POST: PRODUCTS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Price,PromoPrice,Description,Ingredient,ImageUrl,CategoryId,BrandId,Stock,IsVegan,ExpiryDate,IsActive,SkinType,SKU,Barcode,Slug,VideoUrl,Tags,MetaTitle,MetaDescription,MetaKeywords")] Product product)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Categories = _context.Categories.ToList();
        ViewBag.Brands = _context.Brands.ToList();
        return View(product);
    }

    // GET: PRODUCTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // POST: PRODUCTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int? id)
    {
        return _context.Products.Any(e => e.Id == id);
    }

    // ================= PRODUCT VARIANTS API (AJAX) =================

    [HttpGet]
    public async Task<IActionResult> GetVariants(int productId)
    {
        var variants = await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Name)
            .ToListAsync();
        return Json(variants);
    }

    [HttpPost]
    public async Task<IActionResult> AddVariant(int productId, string name, string value, decimal priceAdjustment, int stock)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
        {
            return Json(new { success = false, message = "Tên thuộc tính và Giá trị không được để trống." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại." });
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            Name = name,
            Value = value,
            PriceAdjustment = priceAdjustment,
            Stock = stock
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Đã thêm biến thể thành công!", variant });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVariant(int variantId)
    {
        var variant = await _context.ProductVariants.FindAsync(variantId);
        if (variant == null)
        {
            return Json(new { success = false, message = "Biến thể không tồn tại." });
        }

        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Đã xóa biến thể thành công!" });
    }
}
