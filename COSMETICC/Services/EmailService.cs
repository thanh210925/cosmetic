using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace COSMETICC.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var fromEmail = _config["Email:From"] ?? "YOUR_GMAIL@gmail.com";
            var appPassword = _config["Email:AppPassword"] ?? "";

            if (appPassword == "" || fromEmail.StartsWith("YOUR"))
            {
                // Fallback: log to console instead of sending
                _logger.LogInformation("📧 [EMAIL MOCK] To: {To} | Subject: {Subject}", toEmail, subject);
                _logger.LogInformation("📧 [EMAIL BODY PREVIEW]:\n{Body}", htmlBody.Length > 300 ? htmlBody.Substring(0, 300) + "..." : htmlBody);
                return;
            }

            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromEmail, appPassword)
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, "MeiLing Cosmetics"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mail.To.Add(new MailAddress(toEmail, toName));

                await client.SendMailAsync(mail);
                _logger.LogInformation("📧 Email sent to {To} — {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email to {To}", toEmail);
            }
        }

        // ─── Email xác nhận đặt hàng ────────────────────────────────────
        public Task SendOrderConfirmationAsync(string toEmail, string toName, string orderCode, decimal total, string items)
        {
            var subject = $"✅ Xác nhận đơn hàng #{orderCode} — MeiLing Cosmetics";
            var html = $@"
<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;background:#f8fafc;margin:0;padding:0;'>
<div style='max-width:600px;margin:40px auto;background:white;border-radius:16px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
  <div style='background:linear-gradient(135deg,#800020,#c41e3a);padding:32px;text-align:center;'>
    <h1 style='color:white;margin:0;font-size:24px;'>MeiLing Cosmetics</h1>
    <p style='color:rgba(255,255,255,0.8);margin:8px 0 0;font-size:14px;'>Đơn hàng của bạn đã được xác nhận!</p>
  </div>
  <div style='padding:32px;'>
    <p style='color:#1e293b;font-size:16px;'>Xin chào <strong>{toName}</strong>,</p>
    <p style='color:#475569;'>Cảm ơn bạn đã tin tưởng mua sắm tại MeiLing! Đơn hàng của bạn đã được ghi nhận thành công.</p>
    
    <div style='background:#f0fdf4;border:1px solid #bbf7d0;border-radius:12px;padding:20px;margin:20px 0;'>
      <div style='display:flex;justify-content:space-between;margin-bottom:8px;'>
        <span style='color:#64748b;'>Mã đơn hàng:</span>
        <strong style='color:#800020;font-size:18px;'>#{orderCode}</strong>
      </div>
      <div style='display:flex;justify-content:space-between;'>
        <span style='color:#64748b;'>Tổng tiền:</span>
        <strong style='color:#1e293b;font-size:18px;'>{total:N0}₫</strong>
      </div>
    </div>

    <p style='color:#475569;font-size:14px;'><strong>Sản phẩm:</strong></p>
    <div style='background:#f8fafc;border-radius:8px;padding:16px;color:#475569;font-size:14px;'>{items}</div>

    <div style='text-align:center;margin-top:28px;'>
      <a href='http://localhost:5000/Cart/OrderHistory' style='background:linear-gradient(135deg,#800020,#c41e3a);color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:700;display:inline-block;'>
        Xem chi tiết đơn hàng →
      </a>
    </div>
    
    <p style='color:#94a3b8;font-size:12px;text-align:center;margin-top:24px;'>
      MeiLing Cosmetics — Tỏa sáng rạng rỡ 🌸<br/>
      Nếu có thắc mắc, vui lòng liên hệ support@meilingcosmetics.vn
    </p>
  </div>
</div>
</body></html>";
            return SendAsync(toEmail, toName, subject, html);
        }

        // ─── Email OTP ────────────────────────────────────────────────────
        public Task SendOtpAsync(string toEmail, string toName, string otp, string purpose = "xác minh tài khoản")
        {
            var subject = $"🔐 Mã OTP {otp} — MeiLing Cosmetics";
            var html = $@"
<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;background:#f8fafc;margin:0;padding:20px;'>
<div style='max-width:480px;margin:0 auto;background:white;border-radius:16px;padding:40px;box-shadow:0 4px 20px rgba(0,0,0,0.08);text-align:center;'>
  <div style='font-size:48px;margin-bottom:16px;'>🔐</div>
  <h2 style='color:#1e293b;margin:0 0 8px;'>Mã xác minh của bạn</h2>
  <p style='color:#64748b;font-size:14px;'>Dùng để {purpose}</p>
  
  <div style='background:linear-gradient(135deg,#f0f4ff,#fdf4ff);border:2px dashed #7c3aed;border-radius:12px;padding:24px;margin:24px 0;'>
    <div style='font-size:42px;font-weight:900;letter-spacing:12px;color:#7c3aed;'>{otp}</div>
  </div>
  
  <p style='color:#ef4444;font-size:13px;'>⏰ Mã có hiệu lực trong <strong>5 phút</strong>. Không chia sẻ mã này với bất kỳ ai!</p>
  <p style='color:#94a3b8;font-size:12px;margin-top:16px;'>MeiLing Cosmetics 🌸</p>
</div>
</body></html>";
            return SendAsync(toEmail, toName, subject, html);
        }

        // ─── Email nhắc đánh giá ──────────────────────────────────────────
        public Task SendReviewReminderAsync(string toEmail, string toName, string orderCode, string productNames)
        {
            var subject = $"⭐ Bạn cảm thấy thế nào về đơn hàng #{orderCode}? — MeiLing";
            var html = $@"
<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;background:#f8fafc;margin:0;padding:20px;'>
<div style='max-width:540px;margin:0 auto;background:white;border-radius:16px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
  <div style='background:linear-gradient(135deg,#f59e0b,#d97706);padding:28px;text-align:center;'>
    <div style='font-size:48px;'>⭐</div>
    <h2 style='color:white;margin:8px 0 0;'>Chia sẻ trải nghiệm của bạn!</h2>
  </div>
  <div style='padding:28px;'>
    <p>Xin chào <strong>{toName}</strong>,</p>
    <p style='color:#475569;'>Bạn đã nhận đơn hàng <strong>#{orderCode}</strong>. Đánh giá của bạn giúp chúng tôi cải thiện và giúp những khách hàng khác có thêm thông tin!</p>
    <p style='color:#475569;font-size:14px;'><em>Sản phẩm trong đơn: {productNames}</em></p>
    <div style='text-align:center;margin:24px 0;'>
      <a href='http://localhost:5000/Product/List' style='background:linear-gradient(135deg,#f59e0b,#d97706);color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:700;display:inline-block;'>
        ⭐ Đánh giá ngay
      </a>
    </div>
  </div>
</div>
</body></html>";
            return SendAsync(toEmail, toName, subject, html);
        }

        // ─── Email cảnh báo gian lận ──────────────────────────────────────
        public Task SendFraudAlertToAdminAsync(string adminEmail, int orderId, string reason, string userInfo)
        {
            var subject = $"⚠️ CẢNH BÁO GIAN LẬN — Đơn hàng #{orderId}";
            var html = $@"
<!DOCTYPE html><html><body style='font-family:Arial,sans-serif;background:#fff1f1;padding:20px;'>
<div style='max-width:540px;margin:0 auto;background:white;border-radius:12px;border:2px solid #ef4444;overflow:hidden;'>
  <div style='background:#ef4444;padding:20px;text-align:center;'>
    <h2 style='color:white;margin:0;'>⚠️ CẢNH BÁO GIAN LẬN</h2>
  </div>
  <div style='padding:24px;'>
    <p><strong>Đơn hàng #:</strong> {orderId}</p>
    <p><strong>Lý do nghi ngờ:</strong> {reason}</p>
    <p><strong>Thông tin khách:</strong> {userInfo}</p>
    <p style='color:#94a3b8;font-size:12px;margin-top:16px;'>Vui lòng kiểm tra ngay trong Admin Panel.</p>
  </div>
</div>
</body></html>";
            return SendAsync(adminEmail, "Admin", subject, html);
        }
    }
}
