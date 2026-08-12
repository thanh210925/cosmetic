using System.Collections.Generic;
using System.Threading.Tasks;

namespace COSMETICC.Services
{
    public interface IGhnService
    {
        Task<List<GhnProvince>> GetProvincesAsync();
        Task<List<GhnDistrict>> GetDistrictsAsync(int provinceId);
        Task<List<GhnWard>> GetWardsAsync(int districtId);
        Task<GhnFeeResult> CalculateShippingFeeAsync(int toDistrictId, string toWardCode, int weightGram = 500, decimal insuranceValue = 0);
        Task<GhnCreateOrderResult> CreateOrderAsync(int orderId, string receiverName, string receiverPhone, string address, string wardCode, int districtId, decimal codAmount, int weightGram = 500);
        Task<GhnOrderDetailResult?> GetOrderDetailAsync(string ghnOrderCode);
    }

    public class GhnProvince
    {
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
    }

    public class GhnDistrict
    {
        public int DistrictID { get; set; }
        public int ProvinceID { get; set; }
        public string DistrictName { get; set; } = string.Empty;
    }

    public class GhnWard
    {
        public string WardCode { get; set; } = string.Empty;
        public int DistrictID { get; set; }
        public string WardName { get; set; } = string.Empty;
    }

    public class GhnFeeResult
    {
        public bool Success { get; set; }
        public decimal TotalFee { get; set; }
        public string? ExpectedDeliveryDate { get; set; }
        public string? Message { get; set; }
    }

    public class GhnCreateOrderResult
    {
        public bool Success { get; set; }
        public string? OrderCode { get; set; }
        public decimal TotalFee { get; set; }
        public string? ExpectedDeliveryDate { get; set; }
        public string? Message { get; set; }
    }

    public class GhnOrderDetailResult
    {
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
    }
}
