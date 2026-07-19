using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Product/List
        public async Task<IActionResult> List(
            int? categoryId, 
            int? brandId, 
            decimal? priceMin, 
            decimal? priceMax, 
            string? skinType, 
            string? sort, 
            string? search,
            bool? hasPromo)
        {
            // Base query for products including category, brand, and reviews
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive)
                .AsQueryable();

            // 1. Category Filter
            if (categoryId.HasValue)
            {
                // Support child categories too if parent is selected
                var categoryIds = await _context.Categories
                    .Where(c => c.Id == categoryId || c.ParentCategoryId == categoryId)
                    .Select(c => c.Id)
                    .ToListAsync();
                query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
                ViewBag.CurrentCategoryId = categoryId;
            }

            // 2. Brand Filter
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId);
                ViewBag.CurrentBrandId = brandId;
            }

            // 3. Price Filter
            if (priceMin.HasValue)
            {
                query = query.Where(p => (p.PromoPrice ?? p.Price) >= priceMin.Value);
            }
            if (priceMax.HasValue)
            {
                query = query.Where(p => (p.PromoPrice ?? p.Price) <= priceMax.Value);
            }

            // 4. Skin Type Filter
            if (!string.IsNullOrEmpty(skinType))
            {
                query = query.Where(p => p.SkinType != null && p.SkinType.Contains(skinType));
                ViewBag.CurrentSkinType = skinType;
            }

            // 5. Search keyword
            if (!string.IsNullOrEmpty(search))
            {
                var term = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || 
                                         (p.Brand != null && p.Brand.Name.ToLower().Contains(term)) ||
                                         (p.Category != null && p.Category.Name.ToLower().Contains(term)));
                ViewBag.SearchTerm = search;
            }

            // 6. Promo Filter
            if (hasPromo.HasValue && hasPromo.Value)
            {
                query = query.Where(p => p.PromoPrice.HasValue && p.PromoPrice < p.Price);
                ViewBag.CurrentHasPromo = true;
            }

            // 7. Sorting logic
            switch (sort)
            {
                case "bestseller":
                    query = query.OrderByDescending(p => p.Stock); // Best seller proxy
                    break;
                case "price_asc":
                    query = query.OrderBy(p => p.PromoPrice ?? p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.PromoPrice ?? p.Price);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(p => p.Id);
                    break;
            }

            var products = await query.ToListAsync();

            // Load all categories and brands for the filter sidebar
            ViewBag.Categories = await _context.Categories
                .Include(c => c.ChildCategories)
                .Where(c => c.ParentCategoryId == null) // Load root categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Brands = await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

            // Unique skin types currently in products for dynamic filters
            ViewBag.SkinTypes = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.SkinType))
                .Select(p => p.SkinType)
                .Distinct()
                .ToListAsync();

            // Set current title
            string pageTitle = "Tất cả sản phẩm";
            if (hasPromo.HasValue && hasPromo.Value)
            {
                pageTitle = "Sản phẩm giảm giá";
            }
            else if (categoryId.HasValue)
            {
                var cat = await _context.Categories.FindAsync(categoryId);
                if (cat != null) pageTitle = cat.Name;
            }
            else if (brandId.HasValue)
            {
                var brand = await _context.Brands.FindAsync(brandId);
                if (brand != null) pageTitle = "Thương hiệu: " + brand.Name;
            }
            else if (!string.IsNullOrEmpty(search))
            {
                pageTitle = $"Kết quả tìm kiếm cho: \"{search}\"";
            }

            ViewBag.PageTitle = pageTitle;
            ViewBag.CurrentSort = sort ?? "newest";
            ViewBag.PriceMin = priceMin;
            ViewBag.PriceMax = priceMax;

            return View(products);
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Load suggested products in same category (limit 6)
            ViewBag.SuggestedProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Take(6)
                .ToListAsync();

            // Load suggested products from same brand (limit 6)
            ViewBag.BrandProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.BrandId == product.BrandId && p.Id != product.Id && p.IsActive)
                .Take(6)
                .ToListAsync();

            return View(product);
        }
    }
}
