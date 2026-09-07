using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using COSMETICC.Models;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Services
{
    public class RecommendationService
    {
        private readonly AppDbContext _context;

        public RecommendationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetPersonalizedRecommendationsAsync(int? userId, int take = 8)
        {
            if (userId.HasValue && userId.Value > 0)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user != null)
                {
                    // Fetch categories of past purchased products
                    var userPurchasedCategoryIds = await _context.Orders
                        .Where(o => o.UserId == userId.Value)
                        .SelectMany(o => o.OrderDetails)
                        .Select(od => od.Product != null ? od.Product.CategoryId : (int?)null)
                        .Where(cId => cId.HasValue)
                        .Select(cId => cId.Value)
                        .Distinct()
                        .ToListAsync();

                    var query = _context.Products
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .Include(p => p.ProductImages)
                        .Include(p => p.Reviews)
                        .AsQueryable();

                    var allProducts = await query.ToListAsync();

                    var scoredProducts = allProducts.Select(p =>
                    {
                        double score = 0;

                        // SkinType match
                        if (!string.IsNullOrEmpty(user.SkinType) && !string.IsNullOrEmpty(p.SkinType))
                        {
                            if (p.SkinType.Contains(user.SkinType, StringComparison.OrdinalIgnoreCase) ||
                                p.SkinType.Contains("Mọi loại da", StringComparison.OrdinalIgnoreCase))
                            {
                                score += 10;
                            }
                        }

                        // Category match from purchase history
                        if (p.CategoryId.HasValue && userPurchasedCategoryIds.Contains(p.CategoryId.Value))
                        {
                            score += 5;
                        }

                        // Rating & Reviews boost
                        if (p.Reviews.Any())
                        {
                            score += (double)p.Reviews.Average(r => r.Rating);
                        }

                        return new { Product = p, Score = score };
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Product.Id)
                    .Select(x => x.Product)
                    .Take(take)
                    .ToList();

                    if (scoredProducts.Any())
                    {
                        return scoredProducts;
                    }
                }
            }

            // Fallback for guests or new users: Top rated products
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.Reviews.Select(r => r.Rating).DefaultIfEmpty().Average())
                .ThenByDescending(p => p.Id)
                .Take(take)
                .ToListAsync();
        }
    }
}
