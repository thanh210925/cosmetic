using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using COSMETICC.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace COSMETICC.Services
{
    public class PdfInvoiceService
    {
        public PdfInvoiceService()
        {
            // Set QuestPDF community license (free for open source/community use)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateInvoicePdf(Order order, User user, List<OrderDetail> items)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // ─── Header ───────────────────────────────────────────
                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            // Logo / Company name
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("MeiLing Cosmetics")
                                   .FontSize(22).Bold().FontColor("#800020");
                                col.Item().Text("Tỏa sáng rạng rỡ 🌸")
                                   .FontSize(10).FontColor("#94a3b8");
                                col.Item().Text("Email: support@meilingcosmetics.vn")
                                   .FontSize(9).FontColor("#64748b");
                            });

                            // Invoice title
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("HÓA ĐƠN BÁN HÀNG")
                                   .FontSize(16).Bold().FontColor("#1e293b");
                                col.Item().Text($"Mã đơn: {order.OrderCode ?? $"#{order.Id}"}")
                                   .FontSize(11).FontColor("#7c3aed").Bold();
                                col.Item().Text($"Ngày: {order.OrderDate:dd/MM/yyyy HH:mm}")
                                   .FontSize(9).FontColor("#64748b");
                            });
                        });
                    });

                    // ─── Content ──────────────────────────────────────────
                    page.Content().PaddingTop(20).Column(col =>
                    {
                        // Divider
                        col.Item().BorderBottom(2).BorderColor("#800020").PaddingBottom(8).Text("").FontSize(1);

                        // Customer info
                        col.Item().PaddingTop(12).PaddingBottom(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("THÔNG TIN KHÁCH HÀNG").FontSize(9).Bold().FontColor("#64748b");
                                c.Item().Text(user.FullName ?? user.Username).FontSize(11).Bold();
                                if (!string.IsNullOrEmpty(user.Email))
                                    c.Item().Text(user.Email).FontSize(10).FontColor("#475569");
                                if (!string.IsNullOrEmpty(user.Phone))
                                    c.Item().Text(user.Phone).FontSize(10).FontColor("#475569");
                                if (!string.IsNullOrEmpty(user.Address))
                                    c.Item().Text(user.Address).FontSize(10).FontColor("#475569");
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("TRẠNG THÁI").FontSize(9).Bold().FontColor("#64748b");
                                c.Item().Text(order.Status ?? "Đang xử lý")
                                   .FontSize(11).Bold()
                                   .FontColor(order.Status == "Đã giao" ? "#16a34a" : "#f59e0b");
                            });
                        });

                        // Divider
                        col.Item().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(4).Text("").FontSize(1);

                        // Table header
                        col.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4); // Product name
                                cols.RelativeColumn(1); // Qty
                                cols.RelativeColumn(2); // Unit price
                                cols.RelativeColumn(2); // Total
                            });

                            // Header row
                            table.Header(hdr =>
                            {
                                hdr.Cell().Background("#800020").Padding(8)
                                   .Text("SẢN PHẨM").FontColor(Colors.White).Bold().FontSize(9);
                                hdr.Cell().Background("#800020").Padding(8).AlignCenter()
                                   .Text("SL").FontColor(Colors.White).Bold().FontSize(9);
                                hdr.Cell().Background("#800020").Padding(8).AlignRight()
                                   .Text("ĐƠN GIÁ").FontColor(Colors.White).Bold().FontSize(9);
                                hdr.Cell().Background("#800020").Padding(8).AlignRight()
                                   .Text("THÀNH TIỀN").FontColor(Colors.White).Bold().FontSize(9);
                            });

                            // Data rows
                            bool alternate = false;
                            foreach (var item in items)
                            {
                                var bg = alternate ? "#f8fafc" : (string)Colors.White;
                                alternate = !alternate;
                                var lineTotal = (item.Price ?? 0) * (item.Quantity ?? 1);

                                table.Cell().Background(bg).Padding(8)
                                     .Text(item.Product?.Name ?? $"Sản phẩm #{item.ProductId}").FontSize(10);
                                table.Cell().Background(bg).Padding(8).AlignCenter()
                                     .Text((item.Quantity ?? 1).ToString()).FontSize(10);
                                table.Cell().Background(bg).Padding(8).AlignRight()
                                     .Text($"{item.Price:N0}₫").FontSize(10);
                                table.Cell().Background(bg).Padding(8).AlignRight()
                                     .Text($"{lineTotal:N0}₫").FontSize(10).Bold();
                            }
                        });

                        // Totals
                        col.Item().PaddingTop(12).AlignRight().Column(totals =>
                        {
                            var subtotal = items.Sum(i => (i.Price ?? 0) * (i.Quantity ?? 1));

                            totals.Item().BorderTop(1).BorderColor("#e2e8f0").PaddingTop(8).Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Tạm tính:").FontColor("#64748b");
                                r.ConstantItem(120).AlignRight().Text($"{subtotal:N0}₫");
                            });

                            totals.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().AlignRight().Text("Phí vận chuyển:").FontColor("#64748b");
                                r.ConstantItem(120).AlignRight().Text("Miễn phí").FontColor("#16a34a");
                            });

                            totals.Item().PaddingTop(8).BorderTop(2).BorderColor("#800020").Row(r =>
                            {
                                r.RelativeItem().AlignRight()
                                   .Text("TỔNG CỘNG:").FontSize(13).Bold().FontColor("#800020");
                                r.ConstantItem(140).AlignRight()
                                   .Text($"{order.TotalAmount ?? subtotal:N0}₫").FontSize(14).Bold().FontColor("#800020");
                            });
                        });

                        // Notes
                        if (!string.IsNullOrEmpty(order.Notes))
                        {
                            col.Item().PaddingTop(16).Column(c =>
                            {
                                c.Item().Text("Ghi chú:").FontSize(9).Bold().FontColor("#64748b");
                                c.Item().Text(order.Notes).FontSize(10).FontColor("#475569");
                            });
                        }
                    });

                    // ─── Footer ───────────────────────────────────────────
                    page.Footer().AlignCenter().Column(col =>
                    {
                        col.Item().BorderTop(1).BorderColor("#e2e8f0").PaddingTop(8)
                           .Text("Cảm ơn bạn đã tin tưởng mua sắm tại MeiLing Cosmetics! 🌸")
                           .FontSize(10).FontColor("#94a3b8").Italic();
                        col.Item().Text("Mọi thắc mắc vui lòng liên hệ: support@meilingcosmetics.vn")
                           .FontSize(9).FontColor("#cbd5e1");
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
