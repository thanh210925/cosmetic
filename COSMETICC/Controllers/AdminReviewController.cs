using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminReviewController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminReviewController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminReview
        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        // POST: AdminReview/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            review.IsApproved = true;
            review.IsSpam = false;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Duyệt đánh giá thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminReview/Spam/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Spam(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            review.IsSpam = true;
            review.IsApproved = false;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã đánh dấu báo cáo Spam đánh giá này!";
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminReview/Reply/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyText)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            review.ReplyText = replyText;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Phản hồi đánh giá thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminReview/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa đánh giá thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
