using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class CompareController : Controller
    {
        private readonly AppDbContext _context;

        public CompareController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Compare?id1=1&id2=2
        public async Task<IActionResult> Index(int? id1, int? id2)
        {
            // Load list of all active products for the selectors
            var allProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.AllProducts = allProducts;

            Product? p1 = null;
            Product? p2 = null;

            if (id1.HasValue)
            {
                p1 = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(p => p.Id == id1 && p.IsActive);
            }

            if (id2.HasValue)
            {
                p2 = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(p => p.Id == id2 && p.IsActive);
            }

            ViewBag.Product1 = p1;
            ViewBag.Product2 = p2;

            return View();
        }
    }
}
