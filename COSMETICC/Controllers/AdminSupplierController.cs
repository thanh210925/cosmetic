using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminSupplierController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminSupplierController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminSupplier
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return View(suppliers);
        }

        // GET: AdminSupplier/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: AdminSupplier/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminSupplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Phone,Address,ContractDetails")] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedAt = DateTime.Now;
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm nhà cung cấp mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: AdminSupplier/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        // POST: AdminSupplier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Phone,Address,ContractDetails")] Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
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
            return View(supplier);
        }

        // POST: AdminSupplier/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa nhà cung cấp thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}
