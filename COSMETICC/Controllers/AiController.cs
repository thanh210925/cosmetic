using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AiController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new HttpClient();

        // ─── Ingredient Knowledge Base ───────────────────────────────────────
        private static readonly Dictionary<string, string> IngredientInfo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Niacinamide"] = "Thu nhỏ lỗ chân lông, kiểm soát dầu, làm đều màu da, giảm thâm nám. Phù hợp hầu hết các loại da.",
            ["Retinol"] = "Chống lão hóa mạnh, kích thích tái tạo tế bào, giảm nếp nhăn và thâm. Cần thận trọng với da nhạy cảm.",
            ["Vitamin C"] = "Chống oxy hóa, làm sáng da, giảm nám, bảo vệ trước tia UV. Hiệu quả nhất khi dùng buổi sáng.",
            ["Hyaluronic Acid"] = "Cấp ẩm sâu, giữ ẩm tối ưu, da mọng nước và căng bóng. An toàn cho mọi loại da kể cả da nhạy cảm.",
            ["Ceramide"] = "Phục hồi và củng cố hàng rào bảo vệ da, giữ ẩm, giảm kích ứng. Lý tưởng cho da khô và da nhạy cảm.",
            ["AHA"] = "Tẩy tế bào chết nhẹ nhàng, làm sáng da, cải thiện kết cấu da. Glycolic acid, Lactic acid thuộc nhóm này.",
            ["BHA"] = "Salicylic Acid — thông lỗ chân lông, chống mụn, kháng khuẩn nhẹ. Phù hợp cho da dầu mụn.",
            ["Salicylic Acid"] = "Tẩy da chết trong lỗ chân lông, kiểm soát mụn đầu đen và mụn viêm. Dùng nồng độ 0.5-2% là an toàn.",
            ["Glycolic Acid"] = "AHA phổ biến nhất, tẩy tế bào chết hiệu quả, làm mờ nếp nhăn và vết thâm sạm.",
            ["Peptide"] = "Kích thích sản xuất collagen, chống lão hóa, giảm nếp nhăn. Nhẹ nhàng hơn Retinol.",
            ["SPF"] = "Kem chống nắng — bảo vệ da khỏi tia UV gây lão hóa và ung thư da. Không thể thiếu trong routine.",
            ["Centella"] = "Rau má — làm lành da, chống viêm, phục hồi da tổn thương. Dịu nhẹ cho da nhạy cảm.",
            ["Tranexamic Acid"] = "Trị nám, thâm sau mụn, làm đều màu da. Nhẹ hơn Hydroquinone và an toàn hơn.",
            ["Kojic Acid"] = "Làm sáng da, ức chế sản sinh melanin, trị nám. Thường kết hợp với Vitamin C.",
            ["Tea Tree"] = "Kháng khuẩn tự nhiên, giảm mụn viêm, kháng viêm. Cần pha loãng, không dùng nguyên chất.",
        };

        // ─── Skincare Routine Templates ──────────────────────────────────────
        private static readonly Dictionary<string, List<(string Step, string Icon, string Desc)>> RoutineTemplates = new()
        {
            ["Da dầu"] = new()
            {
                ("Sữa rửa mặt tạo bọt", "💧", "Loại bỏ dầu thừa, sạch sâu lỗ chân lông mà không khô căng."),
                ("Toner cân bằng da", "🌿", "Cân bằng pH, se khít lỗ chân lông, kiểm soát bóng dầu."),
                ("Serum Niacinamide hoặc BHA", "✨", "Thu nhỏ lỗ chân lông, kiểm soát dầu tiết, giảm mụn."),
                ("Kem dưỡng ẩm dạng gel", "💦", "Cấp ẩm nhẹ nhàng, không gây bí tắc lỗ chân lông."),
                ("Kem chống nắng dạng lỏng SPF 50+", "☀️", "Bảo vệ da, ưu tiên loại không gây nhờn rít."),
            },
            ["Da khô"] = new()
            {
                ("Sữa rửa mặt dạng kem dịu nhẹ", "💧", "Làm sạch nhẹ nhàng, không làm mất đi lớp lipid bảo vệ da."),
                ("Toner cấp ẩm Hyaluronic Acid", "🌿", "Bổ sung độ ẩm ngay sau khi rửa mặt khi da còn ẩm."),
                ("Serum Hyaluronic Acid hoặc Peptide", "✨", "Cấp nước sâu, kích thích phục hồi da, chống lão hóa."),
                ("Kem dưỡng ẩm đậm đặc Ceramide", "💦", "Khóa ẩm, củng cố hàng rào bảo vệ da, giảm tình trạng khô căng."),
                ("Kem chống nắng dạng kem dưỡng SPF 50+", "☀️", "Bảo vệ da và cấp thêm độ ẩm trong một bước."),
            },
            ["Da hỗn hợp"] = new()
            {
                ("Gel rửa mặt cân bằng", "💧", "Làm sạch đồng đều vùng T dầu và vùng má khô."),
                ("Toner không cồn", "🌿", "Cân bằng pH, chuẩn bị da hấp thụ dưỡng chất tốt hơn."),
                ("Serum Niacinamide", "✨", "Cân bằng bã nhờn, se lỗ chân lông, làm đều màu da toàn mặt."),
                ("Kem dưỡng ẩm nhẹ", "💦", "Dưỡng ẩm mà không gây nhờn vùng T, nuôi dưỡng vùng má."),
                ("Kem chống nắng SPF 50+", "☀️", "Phủ đều, không gây bóng dầu quá mức."),
            },
            ["Da nhạy cảm"] = new()
            {
                ("Sữa rửa mặt Micellar siêu nhẹ", "💧", "Không xà phòng, không cồn, không perfume — cực kỳ dịu nhẹ."),
                ("Toner Centella hoặc Aloe Vera", "🌿", "Làm dịu da, giảm đỏ, phục hồi da tổn thương."),
                ("Serum Ceramide hoặc Centella", "✨", "Củng cố hàng rào da, chống viêm, phục hồi tổn thương."),
                ("Kem dưỡng dịu nhẹ không mùi", "💦", "Nuôi dưỡng và bảo vệ da mà không gây kích ứng."),
                ("Kem chống nắng khoáng chất SPF 50+", "☀️", "Zinc Oxide/Titanium Dioxide — ít kích ứng nhất cho da nhạy cảm."),
            },
            ["Da mụn"] = new()
            {
                ("Sữa rửa mặt Salicylic Acid", "💧", "Thông sạch lỗ chân lông, loại bỏ cặn bẩn gây mụn."),
                ("Toner BHA hoặc Tea Tree", "🌿", "Kháng khuẩn, kiểm soát mụn, giảm viêm."),
                ("Serum Niacinamide + Zinc", "✨", "Kiểm soát dầu, giảm viêm mụn, làm mờ thâm sau mụn."),
                ("Kem dưỡng ẩm không tắc lỗ chân lông", "💦", "Non-comedogenic — cấp ẩm mà không làm nặng thêm tình trạng mụn."),
                ("Kem chống nắng không gây mụn SPF 50+", "☀️", "Oil-free, non-comedogenic — không làm tắc lỗ chân lông."),
            },
        };

        public AiController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ====================================================================
        // 1. AI HUB — Trang trung tâm AI
        // ====================================================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ====================================================================
        // 2. AI GỢI Ý SẢN PHẨM — Dựa trên lịch sử mua hàng
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Recommendations()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            List<Product> recommended;
            string reason = "Sản phẩm được yêu thích nhất";

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                // Lấy danh sách categories và brands đã mua
                var purchasedProductIds = await _context.OrderDetails
                    .Where(od => od.Order.UserId == userId)
                    .Select(od => od.ProductId)
                    .Distinct()
                    .ToListAsync();

                var purchasedProducts = await _context.Products
                    .Where(p => purchasedProductIds.Contains(p.Id))
                    .ToListAsync();

                var favCategoryIds = purchasedProducts
                    .Where(p => p.CategoryId.HasValue)
                    .GroupBy(p => p.CategoryId!.Value)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList();

                var favBrandIds = purchasedProducts
                    .Where(p => p.BrandId.HasValue)
                    .GroupBy(p => p.BrandId!.Value)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList();

                // Gợi ý sản phẩm trong categories/brands yêu thích chưa mua
                recommended = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive &&
                           !purchasedProductIds.Contains(p.Id) &&
                           (favCategoryIds.Contains(p.CategoryId ?? 0) ||
                            favBrandIds.Contains(p.BrandId ?? 0)))
                    .OrderByDescending(p => p.Reviews.Count)
                    .Take(12)
                    .ToListAsync();

                if (recommended.Count > 0)
                    reason = $"Dựa trên {purchasedProductIds.Count} sản phẩm bạn đã mua";

                // Nếu không đủ, bổ sung sản phẩm phổ biến
                if (recommended.Count < 8)
                {
                    var supplement = await _context.Products
                        .Include(p => p.Brand)
                        .Include(p => p.Category)
                        .Include(p => p.Reviews)
                        .Where(p => p.IsActive && !purchasedProductIds.Contains(p.Id)
                               && !recommended.Select(r => r.Id).Contains(p.Id))
                        .OrderByDescending(p => p.Reviews.Count)
                        .Take(12 - recommended.Count)
                        .ToListAsync();
                    recommended.AddRange(supplement);
                }
            }
            else
            {
                // Chưa đăng nhập — hiển thị sản phẩm phổ biến
                recommended = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.Reviews.Count)
                    .Take(12)
                    .ToListAsync();
            }

            ViewBag.Reason = reason;
            return View(recommended);
        }

        // ====================================================================
        // 3. AI CHATBOT — Gemini AI API
        // ====================================================================

        [HttpPost]
        public async Task<IActionResult> AiChat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return Json(new { success = false, reply = "Bạn chưa nhập tin nhắn." });

            var apiKey = _configuration["GeminiAI:ApiKey"];

            // Build product context from database
            var allProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .Select(p => new {
                    p.Name,
                    BrandName = p.Brand != null ? p.Brand.Name : "",
                    CategoryName = p.Category != null ? p.Category.Name : "",
                    p.Price,
                    p.SkinType,
                    p.Ingredient,
                    p.Description
                })
                .Take(50)
                .ToListAsync();

            var productContext = string.Join("\n", allProducts.Select(p =>
                $"- {p.Name} ({p.BrandName}) | Giá: {p.Price:N0}đ | Da: {p.SkinType} | Thành phần: {p.Ingredient?.Substring(0, Math.Min(p.Ingredient?.Length ?? 0, 80))}"));

            var systemPrompt = $@"Bạn là AI tư vấn viên skincare chuyên nghiệp của cửa hàng mỹ phẩm MeiLing Cosmetics. 
Nhiệm vụ của bạn là tư vấn skincare, phân tích loại da, gợi ý sản phẩm phù hợp từ cửa hàng.
Trả lời bằng tiếng Việt, thân thiện, chuyên nghiệp và ngắn gọn (tối đa 200 từ).
Luôn hướng đến sản phẩm của cửa hàng khi phù hợp.

DANH SÁCH SẢN PHẨM CỬA HÀNG:
{productContext}

Khi gợi ý sản phẩm, hãy đề cập tên sản phẩm cụ thể từ danh sách trên nếu phù hợp.";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                // Fallback: Rule-based response
                var fallback = GetRuleBasedResponse(request.Message);
                return Json(new { success = true, reply = fallback });
            }

            try
            {
                var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = systemPrompt + "\n\nKhách hàng hỏi: " + request.Message }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 500
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(geminiUrl, content);
                var responseStr = await response.Content.ReadAsStringAsync();

                var doc = JsonDocument.Parse(responseStr);
                var reply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Xin lỗi, tôi không thể trả lời lúc này.";

                // Save to ChatMessages
                var userIdStr = HttpContext.Session.GetString("UserId");
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
                    userId = uid;

                var connId = Request.Cookies["ChatConnectionId"] ?? Guid.NewGuid().ToString();
                _context.ChatMessages.Add(new ChatMessage
                {
                    Sender = "Customer",
                    Message = request.Message,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    ConnectionId = connId + "_AI"
                });
                _context.ChatMessages.Add(new ChatMessage
                {
                    Sender = "AI",
                    Message = reply,
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    ConnectionId = connId + "_AI"
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, reply });
            }
            catch (Exception ex)
            {
                var fallback = GetRuleBasedResponse(request.Message);
                return Json(new { success = true, reply = fallback });
            }
        }

        private string GetRuleBasedResponse(string message)
        {
            var msg = message.ToLower();

            if (msg.Contains("da dầu") || msg.Contains("da nhờn"))
                return "Da dầu nên dùng sản phẩm chứa Niacinamide (10%), BHA (Salicylic Acid) để kiểm soát dầu và thu nhỏ lỗ chân lông. Tránh kem dưỡng quá đặc, ưu tiên dạng gel. Chống nắng dạng lỏng SPF50+. 💧";

            if (msg.Contains("da khô") || msg.Contains("khô da") || msg.Contains("da căng"))
                return "Da khô cần bổ sung độ ẩm liên tục! Dùng serum Hyaluronic Acid, kem dưỡng Ceramide đặc hơn, rửa mặt bằng sữa dạng kem nhẹ. Tránh sản phẩm chứa cồn. 🌸";

            if (msg.Contains("mụn") || msg.Contains("acne"))
                return "Với da mụn, hãy dùng Salicylic Acid (BHA) để thông lỗ chân lông, Niacinamide để giảm viêm, Tea Tree kháng khuẩn. Đừng squeeze mụn! Và luôn chống nắng. 🎯";

            if (msg.Contains("nám") || msg.Contains("thâm") || msg.Contains("đốm đen"))
                return "Để trị nám và thâm: dùng Vitamin C buổi sáng (chống oxy hóa, làm sáng), Niacinamide giảm melanin, Tranexamic Acid hoặc Kojic Acid. Quan trọng nhất: chống nắng nghiêm ngặt SPF50+! ✨";

            if (msg.Contains("nhăn") || msg.Contains("lão hóa") || msg.Contains("collagen"))
                return "Chống lão hóa cần: Retinol (buổi tối, bắt đầu nồng độ thấp), Vitamin C (sáng), Peptide, kem mắt riêng. Dùng kem chống nắng mỗi ngày là bước anti-aging quan trọng nhất! 🌟";

            if (msg.Contains("niacinamide"))
                return IngredientInfo["Niacinamide"] + " Tìm sản phẩm chứa Niacinamide tại: /AI/Ingredients?q=Niacinamide";

            if (msg.Contains("retinol"))
                return IngredientInfo["Retinol"] + " Bắt đầu với 0.025% 2-3 lần/tuần, dần tăng lên. Dùng buổi tối và nhớ chống nắng!";

            if (msg.Contains("vitamin c"))
                return IngredientInfo["Vitamin C"] + " Nên dùng dạng L-Ascorbic Acid 10-20% hoặc Ascorbyl Glucoside ổn định hơn.";

            if (msg.Contains("routine") || msg.Contains("chăm sóc da"))
                return "Hãy để tôi đề xuất routine phù hợp với loại da của bạn! Truy cập /AI/Routine hoặc làm bài phân tích da tại /AI/SkinAnalysis nhé. 🌸";

            return "Xin chào! Tôi là AI tư vấn skincare của MeiLing Cosmetics. Bạn có thể hỏi tôi về:\n• Loại da và cách chăm sóc\n• Thành phần mỹ phẩm\n• Routine phù hợp\n• Gợi ý sản phẩm\n\nHãy cho tôi biết vấn đề da của bạn! 💕";
        }

        // ====================================================================
        // 4. AI PHÂN TÍCH LOẠI DA — Quiz thông minh
        // ====================================================================

        [HttpGet]
        public IActionResult SkinAnalysis()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeSkin(
            string q1, string q2, string q3,
            string q4, string q5, string q6)
        {
            // Scoring algorithm
            int oilyScore = 0, dryScore = 0, sensitiveScore = 0, acneScore = 0;

            // Q1: Vào buổi chiều, mặt bạn như thế nào?
            if (q1 == "very_oily") oilyScore += 3;
            else if (q1 == "t_zone_oily") { oilyScore += 1; }
            else if (q1 == "normal") { }
            else if (q1 == "tight_dry") dryScore += 3;

            // Q2: Sau khi rửa mặt, cảm giác da?
            if (q2 == "oily_fast") oilyScore += 2;
            else if (q2 == "comfortable") { }
            else if (q2 == "tight") dryScore += 2;
            else if (q2 == "red_irritated") sensitiveScore += 3;

            // Q3: Da bạn có hay nổi mụn không?
            if (q3 == "frequently") acneScore += 3;
            else if (q3 == "sometimes") acneScore += 1;
            else if (q3 == "rarely") { }
            else if (q3 == "never") dryScore += 1;

            // Q4: Da phản ứng với mỹ phẩm mới?
            if (q4 == "often_react") sensitiveScore += 3;
            else if (q4 == "sometimes") sensitiveScore += 1;
            else if (q4 == "rarely") { }
            else if (q4 == "never") dryScore += 1;

            // Q5: Lỗ chân lông của bạn?
            if (q5 == "very_visible") oilyScore += 2;
            else if (q5 == "visible_t") oilyScore += 1;
            else if (q5 == "small") dryScore += 1;
            else if (q5 == "invisible") dryScore += 2;

            // Q6: Vùng da bạn hay bị vấn đề?
            if (q6 == "oily_acne") { oilyScore += 2; acneScore += 2; }
            else if (q6 == "red_react") sensitiveScore += 2;
            else if (q6 == "dry_flaky") dryScore += 2;
            else if (q6 == "balanced") { }

            // Determine skin type
            string skinType;
            if (acneScore >= 4 && oilyScore >= 3) skinType = "Da mụn";
            else if (sensitiveScore >= 4) skinType = "Da nhạy cảm";
            else if (oilyScore >= 4 && dryScore < 2) skinType = "Da dầu";
            else if (dryScore >= 4 && oilyScore < 2) skinType = "Da khô";
            else skinType = "Da hỗn hợp";

            // Save to user profile if logged in
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.SkinType = skinType;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã lưu loại da <strong>{skinType}</strong> vào hồ sơ của bạn!";
                }
            }

            return RedirectToAction("Routine", new { skinType });
        }

        // ====================================================================
        // 5. AI ROUTINE ĐỀ XUẤT — Theo loại da
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Routine(string? skinType)
        {
            // Auto-detect from user profile
            if (string.IsNullOrEmpty(skinType))
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    skinType = user?.SkinType;
                }
            }

            if (string.IsNullOrEmpty(skinType) || !RoutineTemplates.ContainsKey(skinType))
                skinType = "Da hỗn hợp";

            var routine = RoutineTemplates[skinType];

            // Load matching products for each step keyword
            var matchedProducts = new Dictionary<string, List<Product>>();
            foreach (var (step, _, _) in routine)
            {
                var keywords = step.Split(' ').Where(w => w.Length > 4).ToArray();
                var query = _context.Products
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive);

                if (!string.IsNullOrEmpty(skinType))
                    query = query.Where(p => p.SkinType == null || p.SkinType.Contains(skinType) || p.SkinType.Contains("Mọi loại da"));

                var products = await query
                    .OrderByDescending(p => p.Reviews.Count)
                    .Take(3)
                    .ToListAsync();

                matchedProducts[step] = products;
            }

            ViewBag.SkinType = skinType;
            ViewBag.Routine = routine;
            ViewBag.MatchedProducts = matchedProducts;
            return View();
        }

        // ====================================================================
        // 6. AI TÌM THEO THÀNH PHẦN
        // ====================================================================

        [HttpGet]
        public async Task<IActionResult> Ingredients(string? q)
        {
            ViewBag.Query = q ?? "";
            ViewBag.IngredientInfo = IngredientInfo;

            List<Product> results = new();
            if (!string.IsNullOrWhiteSpace(q))
            {
                results = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive && (
                        (p.Ingredient != null && p.Ingredient.Contains(q)) ||
                        (p.Description != null && p.Description.Contains(q)) ||
                        (p.Tags != null && p.Tags.Contains(q)) ||
                        (p.Name != null && p.Name.Contains(q))
                    ))
                    .OrderByDescending(p => p.Reviews.Count)
                    .Take(24)
                    .ToListAsync();

                ViewBag.IngredientDescription = IngredientInfo.ContainsKey(q)
                    ? IngredientInfo[q]
                    : $"Tìm thấy {results.Count} sản phẩm chứa \"{q}\" trong thành phần.";
            }

            return View(results);
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
    }
}
