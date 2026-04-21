using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Models.Admin;
using UI_Dat_Ve_May_Bay.Models.Common;

namespace UI_Dat_Ve_May_Bay.Api.Admin
{
    public class QuanLyThongKeApi
    {
        private readonly ApiClient _apiClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public QuanLyThongKeApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<QuanLyThongKeDashboardDto?> GetDashboardKpiAsync()
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, "/api/QuanLyThongKe/GetDashboardKpi");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "GetDashboardKpi");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<QuanLyThongKeDashboardDto>>(body, _jsonOptions)
                ?? new ApiResponse<QuanLyThongKeDashboardDto>();

            EnsureApiSuccess(wrapper, "Lay KPI thong ke that bai.");
            return wrapper.Data;
        }

        public async Task<List<QuanLyThongKeDoanhThuNgayDto>> DoanhThuTheoNgayRangeAsync(DateTime? from = null, DateTime? to = null)
        {
            var path = $"/api/QuanLyThongKe/DoanhThuTheoNgayRange?from={FormatDate(from)}&to={FormatDate(to)}";
            using var req = _apiClient.CreateRequest(HttpMethod.Get, path);
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "DoanhThuTheoNgayRange");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<QuanLyThongKeDoanhThuNgayDto>>>(body, _jsonOptions)
                ?? new ApiResponse<List<QuanLyThongKeDoanhThuNgayDto>>();

            EnsureApiSuccess(wrapper, "Lay doanh thu theo ngay that bai.");
            return wrapper.Data ?? new List<QuanLyThongKeDoanhThuNgayDto>();
        }

        public async Task<List<QuanLyThongKeVeNgayDto>> VeTheoNgayRangeAsync(DateTime? from = null, DateTime? to = null)
        {
            var path = $"/api/QuanLyThongKe/VeTheoNgayRange?from={FormatDate(from)}&to={FormatDate(to)}";
            using var req = _apiClient.CreateRequest(HttpMethod.Get, path);
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "VeTheoNgayRange");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<QuanLyThongKeVeNgayDto>>>(body, _jsonOptions)
                ?? new ApiResponse<List<QuanLyThongKeVeNgayDto>>();

            EnsureApiSuccess(wrapper, "Lay thong ke ve theo ngay that bai.");
            return wrapper.Data ?? new List<QuanLyThongKeVeNgayDto>();
        }

        private static string FormatDate(DateTime? value)
            => value.HasValue ? Uri.EscapeDataString(value.Value.ToString("yyyy-MM-dd")) : string.Empty;

        private static void EnsureSuccess(HttpResponseMessage res, string body, string apiName)
        {
            if (res.IsSuccessStatusCode) return;
            throw new Exception($"Loi API {apiName}: {(int)res.StatusCode} - {body}");
        }

        private static void EnsureApiSuccess<T>(ApiResponse<T> wrapper, string fallbackMessage)
        {
            if (wrapper.StatusCode == 0 || wrapper.StatusCode == 200) return;
            throw new Exception(string.IsNullOrWhiteSpace(wrapper.Message) ? fallbackMessage : wrapper.Message);
        }
    }
}
