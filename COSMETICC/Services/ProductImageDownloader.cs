using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Services
{
    public static class ProductImageDownloader
    {
        private static readonly HttpClient _httpClient;

        static ProductImageDownloader()
        {
            _httpClient = new HttpClient();
            // Thiết lập User-Agent giả lập trình duyệt để tránh bị các CDN chặn bot
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public static async Task ConvertAllProductImagesToBase64Async(AppDbContext context)
        {
            // Lấy ra các sản phẩm bắt đầu bằng HSK- mà chưa được convert sang base64
            var products = await context.Products
                .Where(p => p.SKU.StartsWith("HSK-") && !p.ImageUrl.StartsWith("data:image/"))
                .ToListAsync();

            if (products.Count == 0) return;

            foreach (var product in products)
            {
                try
                {
                    string imageUrl = product.ImageUrl;
                    if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("http")) continue;

                    // Tải ảnh về
                    byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                    // Xác định định dạng ảnh để tạo data URI phù hợp
                    string extension = "jpg";
                    if (imageUrl.Contains(".png")) extension = "png";
                    else if (imageUrl.Contains(".avif")) extension = "avif";
                    else if (imageUrl.Contains(".webp")) extension = "webp";

                    string base64String = Convert.ToBase64String(imageBytes);
                    product.ImageUrl = $"data:image/{extension};base64,{base64String}";
                }
                catch (Exception ex)
                {
                    // Nếu lỗi (ví dụ link die), bỏ qua sản phẩm này để xử lý tiếp sản phẩm sau
                    System.Diagnostics.Debug.WriteLine($"Error converting image for {product.Name}: {ex.Message}");
                }
            }

            // Lưu thay đổi vào CSDL
            await context.SaveChangesAsync();
        }
    }
}
