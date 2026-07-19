using COSMETICC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Diagnostics;

namespace COSMETICC.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            // 1. Categories
            viewModel.Categories = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();

            // 2. Bestsellers
            viewModel.Bestsellers = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 4.5)
                .Take(8)
                .ToListAsync();

            // 3. New Products
            viewModel.NewProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .ToListAsync();

            // 4. Flash Sale Product (Sale item with the lowest stock)
            viewModel.FlashSaleProduct = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.IsActive && p.PromoPrice.HasValue && p.PromoPrice < p.Price && p.Stock.HasValue && p.Stock > 0)
                .OrderBy(p => p.Stock)
                .FirstOrDefaultAsync();

            if (viewModel.FlashSaleProduct == null)
            {
                viewModel.FlashSaleProduct = await _context.Products
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive && p.Stock.HasValue && p.Stock > 0)
                    .OrderBy(p => p.Stock)
                    .FirstOrDefaultAsync();
            }

            // 5. Brands
            viewModel.Brands = await _context.Brands.Take(12).ToListAsync();

            // 6. Blog Posts
            viewModel.BlogPosts = await _context.BlogPosts
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.CreatedAt)
                .Take(3)
                .ToListAsync();

            // 7. Featured Reviews (Fetched from Blog Post Comments)
            var blogComments = await _context.BlogPostComments
                .Include(c => c.User)
                .Include(c => c.BlogPost)
                .OrderByDescending(c => c.Id)
                .Take(5)
                .ToListAsync();

            if (blogComments.Any())
            {
                viewModel.FeaturedReviews = blogComments.Select(c => new Review
                {
                    Id = c.Id,
                    Comment = c.Content,
                    GuestName = c.User != null ? c.User.FullName : "Độc giả",
                    Rating = 5, // Default to 5 stars for blog comments
                    CreatedAt = c.CreatedAt
                }).ToList();
            }
            else
            {
                viewModel.FeaturedReviews = new List<Review>();
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
