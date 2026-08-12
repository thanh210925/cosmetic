using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using COSMETICC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace COSMETICC.BackgroundServices
{
    // ────────────────────────────────────────────────────────────────────────
    // FLASH SALE AUTO-DEACTIVATE SERVICE
    // Runs every minute. Checks for expired Flash Sales or Flash Sale Items where
    // SoldQuantity >= QuantityForSale or EndTime < DateTime.Now, and marks them inactive.
    // ────────────────────────────────────────────────────────────────────────
    public class FlashSaleAutoDeactivateService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FlashSaleAutoDeactivateService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public FlashSaleAutoDeactivateService(IServiceScopeFactory scopeFactory, ILogger<FlashSaleAutoDeactivateService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("⚡ FlashSaleAutoDeactivateService started — checking every {Interval} min", CheckInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndDeactivateFlashSalesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ FlashSaleAutoDeactivateService error");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CheckAndDeactivateFlashSalesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.Now;

            // 1. Deactivate expired campaign Flash Sales
            var expiredCampaigns = await db.FlashSales
                .Where(fs => fs.IsActive && fs.EndTime <= now)
                .ToListAsync();

            if (expiredCampaigns.Any())
            {
                foreach (var fs in expiredCampaigns)
                {
                    fs.IsActive = false;
                    _logger.LogInformation("⚡ Auto-deactivated expired Flash Sale campaign ID {Id}: '{Title}'", fs.Id, fs.Title);
                }
            }

            // 2. Deactivate Flash Sale Items where SoldQuantity >= QuantityForSale
            var soldOutItems = await db.FlashSaleItems
                .Where(fsi => fsi.IsActive && fsi.SoldQuantity >= fsi.QuantityForSale)
                .ToListAsync();

            if (soldOutItems.Any())
            {
                foreach (var item in soldOutItems)
                {
                    item.IsActive = false;
                    _logger.LogInformation("⚡ Auto-deactivated sold-out Flash Sale Item ID {Id} (ProductId: {ProductId}, Sold: {Sold}/{Max})",
                        item.Id, item.ProductId, item.SoldQuantity, item.QuantityForSale);
                }
            }

            if (expiredCampaigns.Any() || soldOutItems.Any())
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
