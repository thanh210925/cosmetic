using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminShippingFeeController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminShippingFeeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminShippingFee
        public async Task<IActionResult> Index()
        {
            var fees = await _context.ShippingFees
                .OrderBy(f => f.Region)
                .ToListAsync();
            return View(fees);
        }

        // GET: AdminShippingFee/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminShippingFee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Region,Fee,MinAmountForFreeShipping")] ShippingFee shippingFee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shippingFee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm cấu hình phí ship thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(shippingFee);
        }

        // GET: AdminShippingFee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shippingFee = await _context.ShippingFees.FindAsync(id);
            if (shippingFee == null)
            {
                return NotFound();
            }
            return View(shippingFee);
        }

        // POST: AdminShippingFee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Region,Fee,MinAmountForFreeShipping")] ShippingFee shippingFee)
        {
            if (id != shippingFee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shippingFee);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật phí ship thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShippingFeeExists(shippingFee.Id))
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
            return View(shippingFee);
        }

        // POST: AdminShippingFee/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var shippingFee = await _context.ShippingFees.FindAsync(id);
            if (shippingFee == null)
            {
                return NotFound();
            }

            _context.ShippingFees.Remove(shippingFee);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa cấu hình phí ship thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool ShippingFeeExists(int id)
        {
            return _context.ShippingFees.Any(e => e.Id == id);
        }
    }
}
