using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using COSMETICC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace COSMETICC.Controllers
{
    public class AdminReportController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminReportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportRevenueReport(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now.AddMonths(-1);
            var end = endDate ?? DateTime.Now;

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Báo Cáo Doanh Thu");
                
                // Header Title
                worksheet.Cell(1, 1).Value = "BÁO CÁO DOANH THU CỬA HÀNG COSMETIC";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;

                worksheet.Cell(2, 1).Value = $"Từ ngày: {start:dd/MM/yyyy} - Đến ngày: {end:dd/MM/yyyy}";

                // Table Headers
                string[] headers = { "Mã Đơn", "Khách Hàng", "Ngày Đặt", "Trạng Thái", "Số Sản Phẩm", "Tổng Tiền (VNĐ)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(4, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#800020");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                int row = 5;
                decimal grandTotal = 0;

                foreach (var order in orders)
                {
                    worksheet.Cell(row, 1).Value = order.OrderCode ?? $"#ORD-{order.Id}";
                    worksheet.Cell(row, 2).Value = order.User?.FullName ?? order.User?.Username ?? "Khách vãng lai";
                    worksheet.Cell(row, 3).Value = order.OrderDate?.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 4).Value = order.Status;
                    worksheet.Cell(row, 5).Value = order.OrderDetails.Sum(od => od.Quantity);
                    worksheet.Cell(row, 6).Value = (double)(order.TotalAmount ?? 0);

                    grandTotal += (order.TotalAmount ?? 0);
                    row++;
                }

                // Summary Row
                worksheet.Cell(row, 5).Value = "TỔNG CỘNG:";
                worksheet.Cell(row, 5).Style.Font.Bold = true;
                worksheet.Cell(row, 6).Value = (double)grandTotal;
                worksheet.Cell(row, 6).Style.Font.Bold = true;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryReport()
        {
            var batches = await _context.ProductBatches
                .Include(b => b.Product)
                .ThenInclude(p => p.Category)
                .Include(b => b.ImportReceiptDetail)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Báo Cáo Tồn Kho & Hạn Dùng");

                worksheet.Cell(1, 1).Value = "BÁO CÁO TỒN KHO VÀ HẠN SỬ DỤNG MỸ PHẨM";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;

                string[] headers = { "Mã Lô", "Tên Sản Phẩm", "Danh Mục", "Số Lượng Tồn", "Giá Nhập (VNĐ)", "Hạn Sử Dụng", "Cảnh Báo" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(3, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1b4d3e");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                int row = 4;
                var now = DateTime.Now;

                foreach (var b in batches)
                {
                    string warning = "Bình thường";
                    var daysLeft = (b.ExpiryDate - now).TotalDays;
                    if (daysLeft < 0) warning = "ĐÃ HẾT HẠN";
                    else if (daysLeft <= 90) warning = "SẮP HẾT HẠN (Dưới 3 tháng)";

                    worksheet.Cell(row, 1).Value = b.BatchNumber;
                    worksheet.Cell(row, 2).Value = b.Product?.Name ?? "N/A";
                    worksheet.Cell(row, 3).Value = b.Product?.Category?.Name ?? "N/A";
                    worksheet.Cell(row, 4).Value = b.RemainingQuantity;
                    worksheet.Cell(row, 5).Value = (double)(b.ImportReceiptDetail?.UnitPrice ?? 0);

                    worksheet.Cell(row, 6).Value = b.ExpiryDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 7).Value = warning;


                    if (warning.Contains("ĐÃ HẾT HẠN"))
                    {
                        worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
                        worksheet.Cell(row, 7).Style.Font.Bold = true;
                    }

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoTonKho_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
