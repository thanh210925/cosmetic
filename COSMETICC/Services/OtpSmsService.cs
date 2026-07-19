using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace COSMETICC.Services
{
    /// <summary>
    /// Session-based OTP service — generates 6-digit codes, stores in session, verifies.
    /// </summary>
    public class OtpService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OtpService> _logger;
        private const int OtpExpiryMinutes = 5;

        public OtpService(IHttpContextAccessor httpContextAccessor, ILogger<OtpService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>Generates a 6-digit OTP and stores it in session.</summary>
        public string GenerateOtp(string purpose = "verify")
        {
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.Now.AddMinutes(OtpExpiryMinutes).ToString("o");

            var session = _httpContextAccessor.HttpContext?.Session;
            session?.SetString($"OTP_{purpose}", otp);
            session?.SetString($"OTP_{purpose}_Expiry", expiry);

            _logger.LogInformation("🔐 OTP Generated for purpose '{Purpose}': {OTP} (expires at {Expiry})", purpose, otp, expiry);
            return otp;
        }

        /// <summary>Verifies an OTP. Returns true if valid and not expired.</summary>
        public bool VerifyOtp(string inputOtp, string purpose = "verify")
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return false;

            var storedOtp = session.GetString($"OTP_{purpose}");
            var expiryStr = session.GetString($"OTP_{purpose}_Expiry");

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(expiryStr))
                return false;

            if (!DateTime.TryParse(expiryStr, out var expiry) || DateTime.Now > expiry)
            {
                // Expired — clean up
                session.Remove($"OTP_{purpose}");
                session.Remove($"OTP_{purpose}_Expiry");
                return false;
            }

            if (storedOtp != inputOtp) return false;

            // Valid — clean up after use
            session.Remove($"OTP_{purpose}");
            session.Remove($"OTP_{purpose}_Expiry");
            return true;
        }
    }

    /// <summary>
    /// SMS Service — placeholder with Twilio-ready structure.
    /// Fill in Twilio credentials in appsettings.json to activate.
    /// </summary>
    public class SmsService
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
        private readonly ILogger<SmsService> _logger;

        public SmsService(Microsoft.Extensions.Configuration.IConfiguration config, ILogger<SmsService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task SendSmsAsync(string toPhone, string message)
        {
            var accountSid = _config["Twilio:AccountSid"] ?? "";
            var authToken  = _config["Twilio:AuthToken"] ?? "";
            var fromNumber = _config["Twilio:FromNumber"] ?? "";

            if (string.IsNullOrEmpty(accountSid) || accountSid == "YOUR_TWILIO_SID")
            {
                // Mock mode — log to console
                _logger.LogInformation("📱 [SMS MOCK] To: {Phone} | Message: {Msg}", toPhone, message);
                return;
            }

            // Twilio REST API (no SDK needed, raw HTTP)
            try
            {
                using var http = new System.Net.Http.HttpClient();
                var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var payload = new Dictionary<string, string>
                {
                    ["To"] = toPhone,
                    ["From"] = fromNumber,
                    ["Body"] = message
                };

                var response = await http.PostAsync(
                    $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
                    new System.Net.Http.FormUrlEncodedContent(payload));

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("📱 SMS sent to {Phone}", toPhone);
                else
                    _logger.LogWarning("📱 SMS failed to {Phone}: {Status}", toPhone, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "📱 SMS error to {Phone}", toPhone);
            }
        }

        public System.Threading.Tasks.Task SendOtpSmsAsync(string phone, string otp)
            => SendSmsAsync(phone, $"MeiLing Cosmetics: Ma OTP cua ban la {otp}. Co hieu luc trong 5 phut. Khong chia se ma nay.");

        public System.Threading.Tasks.Task SendOrderConfirmSmsAsync(string phone, string orderCode, decimal total)
            => SendSmsAsync(phone, $"MeiLing: Don hang #{orderCode} da duoc xac nhan. Tong tien: {total:N0} VND. Cam on ban!");
    }
}
