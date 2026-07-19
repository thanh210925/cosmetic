using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminBannerController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminBannerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminBanner
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .OrderBy(b => b.Type)
                .ToListAsync();
            return View(banners);
        }

        // GET: AdminBanner/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminBanner/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ImageUrl,LinkUrl,Type,IsActive")] Banner banner)
        {
            if (ModelState.IsValid)
            {
                _context.Add(banner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm banner thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(banner);
        }

        // GET: AdminBanner/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }
            return View(banner);
        }

        // POST: AdminBanner/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ImageUrl,LinkUrl,Type,IsActive")] Banner banner)
        {
            if (id != banner.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(banner);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật banner thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BannerExists(banner.Id))
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
            return View(banner);
        }

        // POST: AdminBanner/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa banner thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool BannerExists(int id)
        {
            return _context.Banners.Any(e => e.Id == id);
        }
    }
}
