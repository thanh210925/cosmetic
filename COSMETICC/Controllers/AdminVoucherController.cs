using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminVoucherController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminVoucherController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminVoucher
        public async Task<IActionResult> Index()
        {
            var vouchers = await _context.Discounts
                .OrderByDescending(d => d.ExpiryDate)
                .ToListAsync();

            return View(vouchers);
        }

        // GET: AdminVoucher/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminVoucher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Percentage,StartDate,ExpiryDate,UsageLimit")] Discount discount)
        {
            if (ModelState.IsValid)
            {
                // Check if code already exists
                var codeExist = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == discount.Code);
                if (codeExist != null)
                {
                    ModelState.AddModelError("Code", "Mã voucher này đã tồn tại!");
                    return View(discount);
                }

                discount.UsedCount = 0;
                _context.Add(discount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo mã voucher thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(discount);
        }

        // POST: AdminVoucher/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa mã voucher thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
