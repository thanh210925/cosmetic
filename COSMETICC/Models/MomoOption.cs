namespace COSMETICC.Models
{
    public class MomoOption
    {
        public string PartnerCode { get; set; } = "MOMO";
        public string AccessKey { get; set; } = "F8WFBGU2VJH88Q0B";
        public string SecretKey { get; set; } = "K951B6V668B0U8EWAPO591B62F46602E";
        public string PaymentUrl { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
        public string ReturnUrl { get; set; } = "https://localhost:44323/Payment/MomoReturn";
        public string IpnUrl { get; set; } = "https://localhost:44323/Payment/MomoNotify";
        public string RequestType { get; set; } = "captureWallet";
    }

    public class MomoCreatePaymentResponseModel
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public long ResponseTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ResultCode { get; set; }
        public string PayUrl { get; set; } = string.Empty;
        public string Deeplink { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
    }

    public class MomoExecuteResponseModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string TransId { get; set; } = string.Empty;
        public string ResultCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty;
        public string ResponseTime { get; set; } = string.Empty;
    }
}
