namespace COSMETICC.Models
{
    public class GhnSettings
    {
        public string Token { get; set; } = string.Empty;
        public string ShopId { get; set; } = string.Empty;
        public int FromDistrictId { get; set; } = 1442; // Mặc định Quận 1 - TP.HCM
        public string FromWardCode { get; set; } = "20101"; // Mặc định Phường Bến Nghé - Q1
        public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api/v2/"; // Sandbox GHN
    }
}
