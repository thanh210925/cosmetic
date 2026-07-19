using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminCategoriesController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminCategories
        public async Task<IActionResult> Index()    
        {
            var categories = await _context.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        // GET: AdminCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: AdminCategories/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View();
        }

        // POST: AdminCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ParentCategoryId")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm danh mục mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(category);
        }

        // GET: AdminCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            // Exclude current category to prevent circular reference
            ViewBag.Categories = _context.Categories
                .Where(c => c.Id != id)
                .OrderBy(c => c.Name)
                .ToList();
                
            return View(category);
        }

        // POST: AdminCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("Id,Name,Description,ParentCategoryId")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            // Prevent self-referencing circular dependency
            if (category.ParentCategoryId == category.Id)
            {
                ModelState.AddModelError("ParentCategoryId", "Danh mục cha không thể là chính nó!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
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
            
            ViewBag.Categories = _context.Categories
                .Where(c => c.Id != id)
                .OrderBy(c => c.Name)
                .ToList();
                
            return View(category);
        }

        // GET: AdminCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: AdminCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Unlink parent for any child categories
                var children = await _context.Categories.Where(c => c.ParentCategoryId == id).ToListAsync();
                foreach (var child in children)
                {
                    child.ParentCategoryId = null;
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int? id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
