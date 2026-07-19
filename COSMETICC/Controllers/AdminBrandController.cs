using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminBrandController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminBrandController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminBrand
        public async Task<IActionResult> Index()
        {
            var brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();
            return View(brands);
        }

        // GET: AdminBrand/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminBrand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,LogoUrl,Country,Website,BannerUrl")] Brand brand)
        {
            if (ModelState.IsValid)
            {
                _context.Add(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: AdminBrand/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: AdminBrand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,LogoUrl,Country,Website,BannerUrl")] Brand brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
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
            return View(brand);
        }

        // POST: AdminBrand/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            // Unlink any products referencing this brand before deleting it
            var products = await _context.Products.Where(p => p.BrandId == id).ToListAsync();
            foreach (var product in products)
            {
                product.BrandId = null;
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa thương hiệu thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
    }
}
