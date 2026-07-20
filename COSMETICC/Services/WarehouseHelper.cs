using System;
using System.Linq;
using System.Threading.Tasks;
using COSMETICC.Models;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Services
{
    public static class WarehouseHelper
    {
        /// <summary>
        /// Tự động cập nhật Hạn sử dụng (ExpiryDate) của sản phẩm dựa trên lô hàng còn tồn gần hết hạn nhất.
        /// </summary>
        public static async Task UpdateProductExpiryDateAsync(AppDbContext context, int productId)
        {
            var minExpiry = await context.ProductBatches
                .Where(b => b.ProductId == productId && b.RemainingQuantity > 0 && b.ExpiryDate > DateTime.Now)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => (DateTime?)b.ExpiryDate)
                .FirstOrDefaultAsync();

            var product = await context.Products.FindAsync(productId);
            if (product != null)
            {
                product.ExpiryDate = minExpiry;
                context.Products.Update(product);
            }
        }

        /// <summary>
        /// Hoàn trả tồn kho kho hàng & lô sản phẩm (ProductBatches + Products.Stock) khi đơn hàng bị HỦY.
        /// </summary>
        public static async Task RestoreOrderStockAsync(AppDbContext context, int orderId)
        {
            var order = await context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || string.IsNullOrEmpty(order.OrderCode)) return;

            // Check if stock for this order code was already restored to prevent duplicate restoration
            bool alreadyRestored = await context.InventoryLogs
                .AnyAsync(l => l.ReferenceCode == order.OrderCode && l.Type == "CANCEL_RESTORE");

            if (alreadyRestored) return;

            // Find all SELL inventory logs recorded when placing this order
            var sellLogs = await context.InventoryLogs
                .Where(l => l.ReferenceCode == order.OrderCode && l.Type == "SELL")
                .ToListAsync();

            if (sellLogs.Any())
            {
                foreach (var log in sellLogs)
                {
                    var product = await context.Products.FindAsync(log.ProductId);
                    if (product != null)
                    {
                        product.Stock += log.Quantity;
                        context.Products.Update(product);
                    }

                    var batch = await context.ProductBatches
                        .FirstOrDefaultAsync(b => b.ProductId == log.ProductId && b.BatchNumber == log.BatchNumber);

                    if (batch != null)
                    {
                        batch.RemainingQuantity += log.Quantity;
                        context.ProductBatches.Update(batch);
                    }

                    var restoreLog = new InventoryLog
                    {
                        ProductId = log.ProductId,
                        Type = "CANCEL_RESTORE",
                        Quantity = log.Quantity,
                        BatchNumber = log.BatchNumber,
                        ReferenceCode = order.OrderCode,
                        Note = $"Hoàn tồn kho do hủy đơn hàng #{order.OrderCode} (Lô: {log.BatchNumber})",
                        CreatedAt = DateTime.Now
                    };
                    context.InventoryLogs.Add(restoreLog);
                }
            }
            else
            {
                // Fallback: If no SELL logs exist (e.g. legacy test data), restore based on OrderDetails directly
                foreach (var detail in order.OrderDetails)
                {
                    var product = await context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        int qty = detail.Quantity ?? 1;
                        product.Stock += qty;
                        context.Products.Update(product);

                        // Find batch for product
                        var batch = await context.ProductBatches
                            .Where(b => b.ProductId == detail.ProductId)
                            .OrderByDescending(b => b.ExpiryDate)
                            .FirstOrDefaultAsync();

                        if (batch != null)
                        {
                            batch.RemainingQuantity += qty;
                            context.ProductBatches.Update(batch);
                        }

                        var restoreLog = new InventoryLog
                        {
                            ProductId = detail.ProductId,
                            Type = "CANCEL_RESTORE",
                            Quantity = qty,
                            BatchNumber = batch?.BatchNumber ?? "GENERAL",
                            ReferenceCode = order.OrderCode,
                            Note = $"Hoàn tồn kho do hủy đơn hàng #{order.OrderCode}",
                            CreatedAt = DateTime.Now
                        };
                        context.InventoryLogs.Add(restoreLog);
                    }
                }
            }

            await context.SaveChangesAsync();

            // Recalculate expiry dates for affected products
            var productIds = order.OrderDetails.Select(d => d.ProductId).Distinct().ToList();
            foreach (var pId in productIds)
            {
                await UpdateProductExpiryDateAsync(context, pId);
            }
            await context.SaveChangesAsync();
        }
    }
}
