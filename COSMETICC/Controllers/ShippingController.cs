using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using COSMETICC.Services;
using Microsoft.AspNetCore.Mvc;

namespace COSMETICC.Controllers
{
    public class ShippingController : Controller
    {
        private readonly IGhnService _ghnService;
        private readonly HttpClient _httpClient;

        public ShippingController(IGhnService ghnService, IHttpClientFactory httpClientFactory)
        {
            _ghnService = ghnService;
            _httpClient = httpClientFactory.CreateClient();
        }

        // GET: /Shipping/GetProvinces
        [HttpGet]
        public async Task<IActionResult> GetProvinces()
        {
            // 1. Try official GHN API first
            var provinces = await _ghnService.GetProvincesAsync();
            if (provinces != null && provinces.Any())
            {
                return Json(new { success = true, data = provinces });
            }

            // 2. Real-time Automatic API Fetch (Vietnam Open API)
            try
            {
                var response = await _httpClient.GetAsync("https://provinces.open-api.vn/api/?depth=1");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var list = new List<GhnProvince>();

                    foreach (var item in root.EnumerateArray())
                    {
                        list.Add(new GhnProvince
                        {
                            ProvinceID = item.GetProperty("code").GetInt32(),
                            ProvinceName = item.GetProperty("name").GetString()
                        });
                    }

                    if (list.Any())
                    {
                        return Json(new { success = true, data = list.OrderBy(p => p.ProvinceName).ToList() });
                    }
                }
            }
            catch { }

            return Json(new { success = false, message = "Không thể tải danh sách Tỉnh/Thành." });
        }

        // GET: /Shipping/GetDistricts?provinceId=...
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            // 1. Try official GHN API first
            var districts = await _ghnService.GetDistrictsAsync(provinceId);
            if (districts != null && districts.Any())
            {
                return Json(new { success = true, data = districts });
            }

            // 2. Real-time Automatic API Fetch (Vietnam Open API) for selected province
            try
            {
                var response = await _httpClient.GetAsync($"https://provinces.open-api.vn/api/p/{provinceId}?depth=2");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("districts", out var distArray))
                    {
                        var list = new List<GhnDistrict>();
                        foreach (var item in distArray.EnumerateArray())
                        {
                            list.Add(new GhnDistrict
                            {
                                DistrictID = item.GetProperty("code").GetInt32(),
                                ProvinceID = provinceId,
                                DistrictName = item.GetProperty("name").GetString()
                            });
                        }

                        if (list.Any())
                        {
                            return Json(new { success = true, data = list.OrderBy(d => d.DistrictName).ToList() });
                        }
                    }
                }
            }
            catch { }

            return Json(new { success = false, message = "Không thể tải danh sách Quận/Huyện." });
        }

        // GET: /Shipping/GetWards?districtId=...
        [HttpGet]
        public async Task<IActionResult> GetWards(int districtId)
        {
            // 1. Try official GHN API first
            var wards = await _ghnService.GetWardsAsync(districtId);
            if (wards != null && wards.Any())
            {
                return Json(new { success = true, data = wards });
            }

            // 2. Real-time Automatic API Fetch (Vietnam Open API) for selected district
            try
            {
                var response = await _httpClient.GetAsync($"https://provinces.open-api.vn/api/d/{districtId}?depth=2");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("wards", out var wardArray))
                    {
                        var list = new List<GhnWard>();
                        foreach (var item in wardArray.EnumerateArray())
                        {
                            list.Add(new GhnWard
                            {
                                WardCode = item.GetProperty("code").GetInt32().ToString(),
                                DistrictID = districtId,
                                WardName = item.GetProperty("name").GetString()
                            });
                        }

                        if (list.Any())
                        {
                            return Json(new { success = true, data = list.OrderBy(w => w.WardName).ToList() });
                        }
                    }
                }
            }
            catch { }

            return Json(new { success = false, message = "Không thể tải danh sách Phường/Xã." });
        }

        // POST: /Shipping/CalculateFee
        [HttpPost]
        public async Task<IActionResult> CalculateFee(int districtId, string wardCode, decimal subtotal)
        {
            if (districtId <= 0 || string.IsNullOrEmpty(wardCode))
            {
                return Json(new { success = false, message = "Vui lòng chọn đầy đủ Quận/Huyện và Phường/Xã." });
            }

            var feeResult = await _ghnService.CalculateShippingFeeAsync(districtId, wardCode, weightGram: 500, insuranceValue: subtotal);
            
            decimal fee = 30000m; // Default standard fee
            bool isFreeShipping = subtotal >= 500000m;
            string expectedDate = "2-3 ngày";

            if (feeResult.Success && feeResult.TotalFee > 0)
            {
                fee = feeResult.TotalFee;
                if (feeResult.ExpectedDeliveryDate != null)
                {
                    expectedDate = feeResult.ExpectedDeliveryDate;
                }
            }

            if (isFreeShipping)
            {
                fee = 0m;
            }

            return Json(new
            {
                success = true,
                fee = fee,
                originalFee = feeResult.Success ? feeResult.TotalFee : 30000m,
                isFreeShipping = isFreeShipping,
                expectedDate = expectedDate,
                message = feeResult.Success ? "Tính phí GHN thành công" : "Cước phí chuẩn GHN (Dự kiến giao 2-3 ngày)"
            });
        }
    }
}
