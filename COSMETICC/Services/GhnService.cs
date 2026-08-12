using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using COSMETICC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COSMETICC.Services
{
    public class GhnService : IGhnService
    {
        private readonly HttpClient _httpClient;
        private readonly GhnSettings _settings;
        private readonly ILogger<GhnService> _logger;

        public GhnService(HttpClient httpClient, IOptions<GhnSettings> settings, ILogger<GhnService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
                ? "https://dev-online-gateway.ghn.vn/shiip/public-api/v2/"
                : _settings.BaseUrl;

            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            if (!string.IsNullOrEmpty(_settings.Token))
            {
                _httpClient.DefaultRequestHeaders.Add("Token", _settings.Token);
            }
        }

        public async Task<List<GhnProvince>> GetProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("master-data/province");
                if (!response.IsSuccessStatusCode) return new List<GhnProvince>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200 && root.TryGetProperty("data", out var data))
                {
                    var result = JsonSerializer.Deserialize<List<GhnProvince>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result ?? new List<GhnProvince>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GHN provinces");
            }
            return new List<GhnProvince>();
        }

        public async Task<List<GhnDistrict>> GetDistrictsAsync(int provinceId)
        {
            try
            {
                var payload = new { province_id = provinceId };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("master-data/district", content);
                if (!response.IsSuccessStatusCode) return new List<GhnDistrict>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200 && root.TryGetProperty("data", out var data))
                {
                    var result = JsonSerializer.Deserialize<List<GhnDistrict>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result ?? new List<GhnDistrict>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GHN districts for province {ProvinceId}", provinceId);
            }
            return new List<GhnDistrict>();
        }

        public async Task<List<GhnWard>> GetWardsAsync(int districtId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"master-data/ward?district_id={districtId}");
                if (!response.IsSuccessStatusCode) return new List<GhnWard>();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200 && root.TryGetProperty("data", out var data))
                {
                    var result = JsonSerializer.Deserialize<List<GhnWard>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result ?? new List<GhnWard>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GHN wards for district {DistrictId}", districtId);
            }
            return new List<GhnWard>();
        }

        public async Task<GhnFeeResult> CalculateShippingFeeAsync(int toDistrictId, string toWardCode, int weightGram = 500, decimal insuranceValue = 0)
        {
            try
            {
                var payload = new
                {
                    from_district_id = _settings.FromDistrictId,
                    from_ward_code = _settings.FromWardCode,
                    service_type_id = 2, // Standard e-commerce express shipping
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    height = 10,
                    length = 20,
                    weight = weightGram,
                    width = 15,
                    insurance_value = (int)insuranceValue
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "shipping-order/fee");
                if (!string.IsNullOrEmpty(_settings.ShopId))
                {
                    request.Headers.Add("ShopId", _settings.ShopId);
                }
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200 && root.TryGetProperty("data", out var data))
                {
                    decimal totalFee = 0;
                    if (data.TryGetProperty("total", out var totalProp))
                    {
                        totalFee = totalProp.GetDecimal();
                    }

                    return new GhnFeeResult
                    {
                        Success = true,
                        TotalFee = totalFee,
                        ExpectedDeliveryDate = DateTime.Now.AddDays(2).ToString("dd/MM/yyyy")
                    };
                }
                else
                {
                    var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Lỗi tính phí GHN";
                    return new GhnFeeResult { Success = false, Message = msg };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating GHN fee");
                return new GhnFeeResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<GhnCreateOrderResult> CreateOrderAsync(int orderId, string receiverName, string receiverPhone, string address, string wardCode, int districtId, decimal codAmount, int weightGram = 500)
        {
            try
            {
                var payload = new
                {
                    payment_type_id = 2, // 2: Người nhận thanh toán phí ship / COD
                    note = "Đơn hàng mỹ phẩm MeiLing Cosmetics - Cho xem hàng không cho thử",
                    required_note = "CHOXEMHANGKHONGTHU",
                    from_name = "MeiLing Cosmetics",
                    from_phone = "0900000000",
                    from_address = "123 Nguyễn Trãi, Phường Bến Nghé, Quận 1",
                    from_ward_name = "Phường Bến Nghé",
                    from_district_name = "Quận 1",
                    from_province_name = "TP Hồ Chí Minh",
                    client_order_code = $"ORDER-{orderId}",
                    to_name = receiverName,
                    to_phone = receiverPhone,
                    to_address = address,
                    to_ward_code = wardCode,
                    to_district_id = districtId,
                    cod_amount = (int)codAmount,
                    weight = weightGram,
                    length = 20,
                    width = 15,
                    height = 10,
                    service_type_id = 2,
                    items = new[]
                    {
                        new { name = $"Đơn hàng #{orderId}", quantity = 1, price = (int)codAmount }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "shipping-order/create");
                if (!string.IsNullOrEmpty(_settings.ShopId))
                {
                    request.Headers.Add("ShopId", _settings.ShopId);
                }
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200 && root.TryGetProperty("data", out var data))
                {
                    string orderCode = data.TryGetProperty("order_code", out var oc) ? oc.GetString() ?? "" : "";
                    decimal totalFee = data.TryGetProperty("total_fee", out var tf) ? tf.GetDecimal() : 0;
                    string expectedDate = data.TryGetProperty("expected_delivery_time", out var edt) ? edt.GetString() ?? "" : "";

                    return new GhnCreateOrderResult
                    {
                        Success = true,
                        OrderCode = orderCode,
                        TotalFee = totalFee,
                        ExpectedDeliveryDate = expectedDate
                    };
                }
                else
                {
                    var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Lỗi tạo vận đơn GHN";
                    return new GhnCreateOrderResult { Success = false, Message = msg };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating GHN shipping order for OrderId {OrderId}", orderId);
                return new GhnCreateOrderResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<GhnOrderDetailResult?> GetOrderDetailAsync(string ghnOrderCode)
        {
            try
            {
                var payload = new { order_code = ghnOrderCode };
                var request = new HttpRequestMessage(HttpMethod.Post, "shipping-order/detail");
                if (!string.IsNullOrEmpty(_settings.ShopId))
                {
                    request.Headers.Add("ShopId", _settings.ShopId);
                }
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200 && root.TryGetProperty("data", out var data))
                {
                    string status = data.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                    string statusName = GetStatusName(status);
                    return new GhnOrderDetailResult
                    {
                        OrderCode = ghnOrderCode,
                        Status = status,
                        StatusName = statusName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GHN order detail for {OrderCode}", ghnOrderCode);
            }
            return null;
        }

        private static string GetStatusName(string status)
        {
            return status.ToLower() switch
            {
                "ready_to_pick" => "Mới tạo - Chờ lấy hàng",
                "picking" => "Đang lấy hàng",
                "storing" => "Đã nhập kho GHN",
                "delivering" => "Đang giao hàng",
                "delivered" => "Giao hàng thành công",
                "cancel" => "Đơn hàng đã hủy",
                "return" => "Đang chuyển hoàn",
                _ => status
            };
        }
    }
}
