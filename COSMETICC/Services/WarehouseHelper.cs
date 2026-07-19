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
    }
}
