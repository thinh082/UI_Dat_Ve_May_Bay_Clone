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
    public class QuanLyPhieuGiamGiaApi
    {
        private readonly ApiClient _apiClient;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public QuanLyPhieuGiamGiaApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<QuanLyPhieuGiamGiaItemDto>> GetDanhSachPhieuGiamGiaAsync()
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, "/api/QuanLyPhieuGiamGia/GetDanhSachPhieuGiamGia");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API GetDanhSachPhieuGiamGia: {(int)res.StatusCode} - {body}");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<QuanLyPhieuGiamGiaItemDto>>>(body, _jsonOptions);
            return wrapper?.Data ?? new List<QuanLyPhieuGiamGiaItemDto>();
        }

        public async Task<ApiResponse<object>> ActivePhieuGiamGiaAsync(long idPhieuGiamGia)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, $"/api/QuanLyPhieuGiamGia/ActivePhieuGiamGia?idPhieuGiamGia={idPhieuGiamGia}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API ActivePhieuGiamGia: {(int)res.StatusCode} - {body}");

            return JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object> { StatusCode = 200, Message = "OK", Data = null };
        }

        public async Task<ApiResponse<object>> CapNhatPhieuGiamGiaAsync(QuanLyPhieuGiamGiaUpsertModel model)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyPhieuGiamGia/CapNhatPhieuGiamGia");
            var json = JsonSerializer.Serialize(model);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API CapNhatPhieuGiamGia: {(int)res.StatusCode} - {body}");

            return JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object> { StatusCode = 200, Message = "OK", Data = null };
        }

        public async Task<ApiResponse<object>> XoaPhieuGiamGiaAsync(long idPhieuGiamGia)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, $"/api/QuanLyPhieuGiamGia/XoaPhieuGiamGia?idPhieuGiamGia={idPhieuGiamGia}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API XoaPhieuGiamGia: {(int)res.StatusCode} - {body}");

            return JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object> { StatusCode = 200, Message = "OK", Data = null };
        }
    }
}
