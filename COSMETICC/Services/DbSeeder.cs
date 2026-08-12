using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Models
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            try
            {
                // 1. Ensure Categories exist
                if (!await db.Categories.AnyAsync())
                {
                    db.Categories.AddRange(
                        new Category { Name = "Chăm sóc da", Description = "Dưỡng da, trị mụn, cấp ẩm" },
                        new Category { Name = "Trang điểm", Description = "Son môi, phấn nền, mascara" },
                        new Category { Name = "Chăm sóc cơ thể", Description = "Sữa tắm, tẩy tế bào chết" }
                    );
                    await db.SaveChangesAsync();
                }

                // 2. If products are empty or fewer than 10, execute seed script or C# seeding
                if (await db.Products.CountAsync() < 10)
                {
                    string sqlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "seed_50_hasaki_products.sql");
                    if (!File.Exists(sqlFilePath))
                    {
                        sqlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "seed_50_hasaki_products.sql");
                    }

                    if (File.Exists(sqlFilePath))
                    {
                        string sql = await File.ReadAllTextAsync(sqlFilePath);
                        // Clean up PRINT statements which SQL Server client doesn't need
                        sql = sql.Replace("PRINT 'Brands Re-Seeded!';", "")
                                 .Replace("PRINT 'Successfully Seeded 50 Hasaki Products!';", "");
                        await db.Database.ExecuteSqlRawAsync(sql);
                    }
                }

                // 3. Update high-quality real cosmetic image URLs for all products
                var products = await db.Products.ToListAsync();
                if (products.Any())
                {
                    var cosmeticImages = new Dictionary<string, string[]>
                    {
                        { "Bioderma", new[] {
                            "https://images.unsplash.com/photo-1556228578-8c89e6adf883?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "La Roche-Posay", new[] {
                            "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1556228578-8c89e6adf883?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "CeraVe", new[] {
                            "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1556228578-8c89e6adf883?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Paula's Choice", new[] {
                            "https://images.unsplash.com/photo-1626784215021-2e39ccf971cd?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1617897903246-719242758050?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Klairs", new[] {
                            "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Skin1004", new[] {
                            "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Anessa", new[] {
                            "https://images.unsplash.com/photo-1526947425960-945c6e72858f?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "L'Oréal Paris", new[] {
                            "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1586495777744-4413f21062fa?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Vichy", new[] {
                            "https://images.unsplash.com/photo-1617897903246-719242758050?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1556228578-8c89e6adf883?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Hada Labo", new[] {
                            "https://images.unsplash.com/photo-1601049541289-9b1b7bbbfe19?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Neutrogena", new[] {
                            "https://images.unsplash.com/photo-1567928257065-c14669877d84?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Innisfree", new[] {
                            "https://images.unsplash.com/photo-1508746829417-e6f548d8d6ed?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Some By Mi", new[] {
                            "https://images.unsplash.com/photo-1616683693504-3ea7e9ad6fec?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?q=80&w=600&auto=format&fit=crop"
                        }},
                        { "Cosrx", new[] {
                            "https://images.unsplash.com/photo-1512290900676-26c2a7a795b1?q=80&w=600&auto=format&fit=crop",
                            "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=600&auto=format&fit=crop"
                        }}
                    };

                    int imgIdx = 0;
                    string[] defaultImgs = new[] {
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=600&auto=format&fit=crop",
                        "https://images.unsplash.com/photo-1626784215021-2e39ccf971cd?q=80&w=600&auto=format&fit=crop",
                        "https://images.unsplash.com/photo-1556228578-8c89e6adf883?q=80&w=600&auto=format&fit=crop",
                        "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?q=80&w=600&auto=format&fit=crop",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?q=80&w=600&auto=format&fit=crop"
                    };

                    foreach (var p in products)
                    {
                        if (string.IsNullOrEmpty(p.ImageUrl) || p.ImageUrl.Contains("hstatic.net") || p.ImageUrl.Contains("placehold.co"))
                        {
                            var brandName = p.Brand?.Name ?? "";
                            if (cosmeticImages.ContainsKey(brandName))
                            {
                                var imgs = cosmeticImages[brandName];
                                p.ImageUrl = imgs[imgIdx % imgs.Length];
                            }
                            else
                            {
                                p.ImageUrl = defaultImgs[imgIdx % defaultImgs.Length];
                            }
                            imgIdx++;
                        }
                    }
                    await db.SaveChangesAsync();
                }

                // 3b. Update Brand Logo URLs
                var allBrands = await db.Brands.ToListAsync();
                var brandLogos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MeiLing", "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?q=80&w=200&auto=format&fit=crop" },
                    { "Cocoon", "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?q=80&w=200&auto=format&fit=crop" },
                    { "Cỏ Mềm", "https://images.unsplash.com/photo-1540555700478-4be289fbecef?q=80&w=200&auto=format&fit=crop" },
                    { "CeraVe", "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c5/CeraVe_logo.svg/320px-CeraVe_logo.svg.png" },
                    { "Bioderma", "https://upload.wikimedia.org/wikipedia/commons/thumb/b/b8/Bioderma_logo.svg/320px-Bioderma_logo.svg.png" },
                    { "La Roche-Posay", "https://upload.wikimedia.org/wikipedia/commons/thumb/0/08/La_Roche-Posay_logo.svg/320px-La_Roche-Posay_logo.svg.png" },
                    { "Paula's Choice", "https://images.unsplash.com/photo-1626784215021-2e39ccf971cd?q=80&w=200&auto=format&fit=crop" },
                    { "Klairs", "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=200&auto=format&fit=crop" },
                    { "Skin1004", "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?q=80&w=200&auto=format&fit=crop" },
                    { "Anessa", "https://images.unsplash.com/photo-1556228578-8c89e6adf883?q=80&w=200&auto=format&fit=crop" },
                    { "L'Oréal Paris", "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9d/L%27Or%C3%A9al_logo.svg/320px-L%27Or%C3%A9al_logo.svg.png" },
                    { "Vichy", "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/Vichy_logo.svg/320px-Vichy_logo.svg.png" }
                };

                bool brandUpdated = false;
                foreach (var b in allBrands)
                {
                    if (string.IsNullOrEmpty(b.LogoUrl) && brandLogos.TryGetValue(b.Name, out var logoUrl))
                    {
                        b.LogoUrl = logoUrl;
                        brandUpdated = true;
                    }
                }
                if (brandUpdated)
                {
                    await db.SaveChangesAsync();
                }

                // 4. Ensure an active Flash Sale exists with items
                var now = DateTime.Now;
                var activeFs = await db.FlashSales.FirstOrDefaultAsync(fs => fs.IsActive && fs.StartTime <= now && fs.EndTime > now);
                if (activeFs == null)
                {
                    activeFs = new FlashSale
                    {
                        Title = "🔥 SUPER FLASH SALE THÁNG 8",
                        StartTime = now.AddHours(-1),
                        EndTime = now.AddDays(7),
                        IsActive = true,
                        CreatedAt = now
                    };
                    db.FlashSales.Add(activeFs);
                    await db.SaveChangesAsync();

                    var seedProducts = await db.Products.Take(6).ToListAsync();
                    foreach (var p in seedProducts)
                    {
                        var fsItem = new FlashSaleItem
                        {
                            FlashSaleId = activeFs.Id,
                            ProductId = p.Id,
                            DiscountPrice = Math.Round((p.PromoPrice ?? p.Price) * 0.7m),
                            QuantityForSale = 30,
                            SoldQuantity = 5,
                            IsActive = true
                        };
                        db.FlashSaleItems.Add(fsItem);
                    }
                    await db.SaveChangesAsync();
                }

                // 5. Ensure at least 5 Beauty Blog Posts exist
                if (await db.BlogPosts.CountAsync() < 5)
                {
                    var sampleBlogs = new List<BlogPost>
                    {
                        new BlogPost
                        {
                            Title = "5 Bước Chuẩn Chăm Sóc Da Dầu Mụn Vào Mùa Hè Oi Nóng",
                            Slug = "5-buoc-chuan-cham-soc-da-dau-mun-vao-mua-he-oi-nong",
                            Summary = "Khám phá quy trình skincare đơn giản nhưng cực kỳ hiệu quả giúp kiềm dầu, giảm mụn và giữ làn da luôn thông thoáng tươi sáng trong thời tiết hè rực rỡ.",
                            Content = @"<h3>1. Tẩy trang dịu nhẹ mỗi tối</h3><p>Dù có trang điểm hay không, việc tẩy trang bằng nước tẩy trang chứa mi-xen dịu nhẹ giúp loại bỏ bã nhờn tích tụ và bụi mịn ô nhiễm sau cả ngày dài ngoài đường.</p><h3>2. Sữa rửa mặt tạo bọt chuẩn pH 5.5</h3><p>Rửa mặt 2 lần mỗi ngày với sữa rửa mặt dịu nhẹ giúp làm sạch sâu mà không gây tổn thương hàng rào bảo vệ da tự nhiên.</p><h3>3. Sử dụng BHA 2% tẩy tế bào chết</h3><p>Salicylic Acid (BHA) đi sâu vào lỗ chân lông làm tan bã nhờn, triệt tiêu vi khuẩn gây mụn trứng cá và mụn đầu đen.</p><h3>4. Cấp ẩm dạng Gel mỏng nhẹ</h3><p>Da dầu vẫn cần cấp nước! Chọn kem dưỡng ẩm dạng gel chứa Hyaluronic Acid để da căng mọng mà không bí tắc.</p><h3>5. Kem chống nắng kiềm dầu khô thoáng</h3><p>Đừng quên thoa kem chống nắng chỉ số SPF 50+ PA++++ trước khi ra ngoài 20 phút để bảo vệ da khỏi thâm sạm và lão hóa sớm.</p>",
                            ImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?q=80&w=800&auto=format&fit=crop",
                            Tags = "Chăm sóc da, Da dầu, Trị mụn, Skincare hè",
                            IsPublished = true,
                            CreatedAt = now.AddDays(-1),
                            LikesCount = 24,
                            CommentsCount = 5,
                            SharesCount = 8
                        },
                        new BlogPost
                        {
                            Title = "Bí Quyết Chọn Kem Chống Nắng Phù Hợp Cho Làn Da Nhạy Cảm",
                            Slug = "bi-quyet-chon-kem-chong-nang-phu-hop-cho-lan-da-nhay-cam",
                            Summary = "Làn da nhạy cảm rất dễ bị kích ứng đỏ rát trước ánh nắng. Bài viết này sẽ hướng dẫn bạn chọn màng lọc chống nắng vật lý và hóa học dịu nhẹ nhất.",
                            Content = @"<h3>Làn da nhạy cảm cần màng lọc chống nắng nào?</h3><p>Các chuyên gia da liễu khuyên dùng kem chống nắng vật lý chứa <strong>Zinc Oxide</strong> và <strong>Titanium Dioxide</strong>. Đây là hai thành phần có khả năng phản xạ tia UV cực tốt mà hoàn toàn không thẩm thấu gây kích ứng da.</p><h3>Tránh xa các thành phần gây dị ứng</h3><p>Hãy kiểm tra bảng thành phần tỉ mỉ để tránh hương liệu nhân tạo (Fragrance/Parfum), cồn khô (Alcohol Denat) và paraben bảo quản mạnh.</p>",
                            ImageUrl = "https://images.unsplash.com/photo-1506744038136-46273834b3fb?q=80&w=800&auto=format&fit=crop",
                            Tags = "Kem chống nắng, Da nhạy cảm, Bảo vệ da, Sunscreen",
                            IsPublished = true,
                            CreatedAt = now.AddDays(-2),
                            LikesCount = 18,
                            CommentsCount = 3,
                            SharesCount = 4
                        },
                        new BlogPost
                        {
                            Title = "Retinol & Niacinamide: Phối Hợp Thần Kỳ Giúp Trẻ Hóa & Mờ Thâm Nám",
                            Slug = "retinol-niacinamide-phoi-hop-than-ky-giup-tre-hoa-mo-tham-nam",
                            Summary = "Bộ đôi hoạt chất vàng trong ngành mỹ phẩm giúp đẩy lùi nếp nhăn, se khít lỗ chân lông và làm đều màu da. Hướng dẫn sử dụng không lo kích ứng.",
                            Content = @"<h3>Tại sao Niacinamide và Retinol lại là bạn đồng hành hoàn hảo?</h3><p>Niacinamide (Vitamin B3) giúp củng cố hàng rào ceramide, làm dịu da và giảm thiểu tình trạng bong tróc đỏ rát thường gặp khi mới bắt đầu sử dụng Retinol.</p><h3>Tần suất sử dụng khuyến nghị</h3><p>Tuần đầu tiên: Dùng Niacinamide mỗi ngày, Retinol 1-2 lần/tuần vào buổi tối. Sau 4 tuần da đã thích ứng, bạn có thể tăng tần suất Retinol lên cách ngày để có làn da mịn màng rạng rỡ.</p>",
                            ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?q=80&w=800&auto=format&fit=crop",
                            Tags = "Retinol, Niacinamide, Trẻ hóa da, Mờ thâm",
                            IsPublished = true,
                            CreatedAt = now.AddDays(-3),
                            LikesCount = 35,
                            CommentsCount = 9,
                            SharesCount = 12
                        },
                        new BlogPost
                        {
                            Title = "Top 5 Thành Phần Dưỡng Ẩm Chuyên Sâu Cho Làn Da Khô Ráp Mùa Hanh Khô",
                            Slug = "top-5-thanh-phan-duong-am-chuyen-sau-cho-lan-da-kho-rap",
                            Summary = "Hyaluronic Acid, Ceramides, Squalane, Glycerin và Bơ ca cao - đâu là cứu tinh tuyệt vời nhất giúp làn da của bạn luôn căng mượt bóng khỏe?",
                            Content = @"<h3>1. Hyaluronic Acid (HA)</h3><p>Khả năng giữ nước gấp 1000 lần trọng lượng của chính nó, giúp bơm căng từng tế bào da thiếu nước.</p><h3>2. Ceramides</h3><p>Tái tạo lớp màng lipid bảo vệ da, ngăn ngừa sự thất thoát độ ẩm ra môi trường bên ngoài.</p><h3>3. Squalane tự nhiên</h3><p>Thành phần tương thích hoàn hảo với tuyến bã nhờn của da, thẩm thấu tức thì mà không gây nhờn dính.</p>",
                            ImageUrl = "https://images.unsplash.com/photo-1515377905703-c4788e51af15?q=80&w=800&auto=format&fit=crop",
                            Tags = "Dưỡng ẩm, Da khô, Hyaluronic Acid, Ceramides",
                            IsPublished = true,
                            CreatedAt = now.AddDays(-4),
                            LikesCount = 29,
                            CommentsCount = 7,
                            SharesCount = 6
                        },
                        new BlogPost
                        {
                            Title = "Bí Quyết Trang Điểm Trong Veo 'No-Makeup' Makeup Chuẩn Phong Cách Hàn Quốc",
                            Slug = "bi-quyet-trang-diem-trong-veo-no-makeup-makeup-chuan-han-quoc",
                            Summary = "Hướng dẫn từng bước đánh lớp nền mỏng nhẹ như sương, đôi môi mọng nước và má hồng tự nhiên cuốn hút ánh nhìn mọi lúc mọi nơi.",
                            Content = @"<h3>1. Lớp nền mỏng nhẹ bóng khỏe (Glass Skin)</h3><p>Dùng Cushion dưỡng ẩm vỗ nhẹ đều khắp mặt, kết hợp che khuyết điểm nhỏ ở vùng mắt và cánh mũi.</p><h3>2. Đôi má hồng ửng nhẹ mộng mơ</h3><p>Chọn phấn má dạng kem tông hồng đào hoặc cam san hô dán nhẹ lên gò má mang lại vẻ tươi tắn rạng ngời.</p><h3>3. Đôi môi mọng nước tràn đầy sức sống</h3><p>Thoa một lớp son dưỡng có màu bóng nhẹ giúp đôi môi trông luôn mềm mượt tự nhiên.</p>",
                            ImageUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?q=80&w=800&auto=format&fit=crop",
                            Tags = "Trang điểm, Makeup Hàn Quốc, Son môi, Lớp nền mỏng nhẹ",
                            IsPublished = true,
                            CreatedAt = now.AddDays(-5),
                            LikesCount = 42,
                            CommentsCount = 11,
                            SharesCount = 15
                        }
                    };

                    db.BlogPosts.AddRange(sampleBlogs);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚡ Error in DbSeeder: " + ex.Message);
            }
        }
    }
}
