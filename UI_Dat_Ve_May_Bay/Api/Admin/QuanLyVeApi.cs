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
    public class QuanLyVeApi
    {
        private readonly ApiClient _apiClient;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public QuanLyVeApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<QuanLyVeDanhSachItemDto>> GetDanhSachDatVeAsync(QuanLyVeLocDatVeAdminModel? filter = null)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyVe/GetDanhSachDatVe");
            var json = JsonSerializer.Serialize(filter ?? new QuanLyVeLocDatVeAdminModel());
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API GetDanhSachDatVe: {(int)res.StatusCode} - {body}");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<QuanLyVeDanhSachItemDto>>>(body, _jsonOptions);
            return wrapper?.Data ?? new List<QuanLyVeDanhSachItemDto>();
        }

        public async Task<QuanLyVeChiTietDto?> GetChiTietDatVeAsync(long idDatVe)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, $"/api/QuanLyVe/GetChiTietDatVe?idDatVe={idDatVe}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API GetChiTietDatVe: {(int)res.StatusCode} - {body}");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<QuanLyVeChiTietDto>>(body, _jsonOptions);
            return wrapper?.Data;
        }

        public async Task<ApiResponse<object>> CapNhatTrangThaiAsync(QuanLyVeCapNhatTrangThaiVeModel model)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyVe/CapNhatTrangThai");
            req.Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API CapNhatTrangThai: {(int)res.StatusCode} - {body}");

            return JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object> { StatusCode = 200, Message = "OK", Data = null };
        }

        public async Task<byte[]> InChiTietVeAsync(long idDatVe)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, $"/api/QuanLyVe/InChiTietVe?idDatVe={idDatVe}");
            using var res = await _apiClient.Http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Loi API InChiTietVe: {(int)res.StatusCode} - {body}");
            }

            return await res.Content.ReadAsByteArrayAsync();
        }
    }
}
