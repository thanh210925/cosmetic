using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace COSMETICC.Services
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOption> _options;
        private readonly HttpClient _httpClient;

        public MomoService(IOptions<MomoOption> options, IHttpClientFactory httpClientFactory)
        {
            _options = options;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(Order order, string? returnUrl = null, string? ipnUrl = null)
        {
            var rawReturnUrl = returnUrl ?? _options.Value.ReturnUrl;
            var rawIpnUrl = ipnUrl ?? _options.Value.IpnUrl;

            long amount = (long)(order.TotalAmount ?? 0m);
            if (amount <= 0) amount = 10000;

            string requestId = Guid.NewGuid().ToString();
            string orderId = order.Id.ToString() + "_" + DateTime.Now.Ticks.ToString().Substring(10);
            string orderInfo = $"Thanh toan don hang #{order.Id} qua Vi MoMo";
            string extraData = "";

            // Raw signature string required by MoMo API v2
            string rawHash = $"accessKey={_options.Value.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={rawIpnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={_options.Value.PartnerCode}&redirectUrl={rawReturnUrl}&requestId={requestId}&requestType={_options.Value.RequestType}";

            string signature = ComputeHmacSha256(rawHash, _options.Value.SecretKey);

            var requestBody = new
            {
                partnerCode = _options.Value.PartnerCode,
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = rawReturnUrl,
                ipnUrl = rawIpnUrl,
                requestType = _options.Value.RequestType,
                extraData = extraData,
                lang = "vi",
                signature = signature
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_options.Value.PaymentUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MomoCreatePaymentResponseModel>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new MomoCreatePaymentResponseModel { ResultCode = -1, Message = "Deserialization failed" };
            }

            return new MomoCreatePaymentResponseModel
            {
                ResultCode = (int)response.StatusCode,
                Message = "Gửi yêu cầu thanh toán MoMo thất bại!"
            };
        }

        public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
        {
            var amount = collection["amount"].ToString();
            var orderId = collection["orderId"].ToString();
            var orderInfo = collection["orderInfo"].ToString();
            var orderType = collection["orderType"].ToString();
            var transId = collection["transId"].ToString();
            var resultCode = collection["resultCode"].ToString();
            var message = collection["message"].ToString();
            var payType = collection["payType"].ToString();
            var responseTime = collection["responseTime"].ToString();

            return new MomoExecuteResponseModel
            {
                Amount = amount,
                OrderId = orderId,
                OrderInfo = orderInfo,
                OrderType = orderType,
                TransId = transId,
                ResultCode = resultCode,
                Message = message,
                PayType = payType,
                ResponseTime = responseTime
            };
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                StringBuilder hex = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                {
                    hex.AppendFormat("{0:x2}", b);
                }
                return hex.ToString();
            }
        }
    }
}
