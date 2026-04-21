using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Models.Admin;
using UI_Dat_Ve_May_Bay.Models.Common;

namespace UI_Dat_Ve_May_Bay.Api.Admin
{
    public class QuanLyChuyenBayApi
    {
        private readonly ApiClient _apiClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public QuanLyChuyenBayApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<QuanLyChuyenBayItemDto>> GetDanhSachChuyenBayAsync(QuanLyChuyenBayFilterModel filter)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyChuyenBay/GetDanhSachChuyenBay");
            req.Content = new StringContent(JsonSerializer.Serialize(filter), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "GetDanhSachChuyenBay");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<QuanLyChuyenBayItemDto>>>(body, _jsonOptions);
            return wrapper?.Data ?? new List<QuanLyChuyenBayItemDto>();
        }

        public async Task<QuanLyChuyenBayDetailDto?> GetChiTietChuyenBayAsync(long idChuyenBay)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, $"/api/QuanLyChuyenBay/GetChiTietChuyenBay?idChuyenBay={idChuyenBay}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "GetChiTietChuyenBay");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<QuanLyChuyenBayDetailDto>>(body, _jsonOptions);
            return wrapper?.Data;
        }

        public async Task<ApiResponse<object>> LuuChuyenBayAsync(QuanLyChuyenBaySaveModel model)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyChuyenBay/LuuChuyenBay");
            req.Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "LuuChuyenBay");

            return JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object> { StatusCode = 200, Message = "OK" };
        }

        public async Task<ApiResponse<object>> XoaChuyenBayAsync(long idChuyenBay)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, $"/api/QuanLyChuyenBay/XoaChuyenBay?idChuyenBay={idChuyenBay}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "XoaChuyenBay");

            return JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object> { StatusCode = 200, Message = "OK" };
        }

        private static void EnsureSuccess(HttpResponseMessage res, string body, string apiName)
        {
            if (res.IsSuccessStatusCode) return;
            throw new Exception($"Loi API {apiName}: {(int)res.StatusCode} - {body}");
        }
    }
}
