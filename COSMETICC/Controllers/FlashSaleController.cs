using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class FlashSaleController : Controller
    {
        private readonly AppDbContext _context;

        public FlashSaleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /FlashSale
        public async Task<IActionResult> Index(int? campaignId)
        {
            var now = DateTime.Now;

            // Fetch active campaigns or upcoming campaigns
            var campaigns = await _context.FlashSales
                .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(fsi => fsi.Product)
                        .ThenInclude(p => p.Brand)
                .Where(fs => fs.IsActive && fs.EndTime > now)
                .OrderBy(fs => fs.StartTime)
                .ToListAsync();

            FlashSale? currentCampaign = null;

            if (campaignId.HasValue)
            {
                currentCampaign = campaigns.FirstOrDefault(c => c.Id == campaignId.Value);
            }

            if (currentCampaign == null)
            {
                // Prefer active campaign (StartTime <= now <= EndTime)
                currentCampaign = campaigns.FirstOrDefault(c => c.StartTime <= now && c.EndTime > now)
                                 ?? campaigns.FirstOrDefault();
            }

            ViewBag.AllCampaigns = campaigns;

            return View(currentCampaign);
        }
    }
}
