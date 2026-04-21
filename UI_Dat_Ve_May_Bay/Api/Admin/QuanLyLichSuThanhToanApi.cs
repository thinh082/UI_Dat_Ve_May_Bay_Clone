using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Models.Admin;
using UI_Dat_Ve_May_Bay.Models.Common;

namespace UI_Dat_Ve_May_Bay.Api.Admin
{
    public class QuanLyLichSuThanhToanApi
    {
        private readonly ApiClient _apiClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public QuanLyLichSuThanhToanApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<QuanLyLichSuThanhToanPagedResult> GetDanhSachLichSuThanhToanAsync(QuanLyLichSuThanhToanFilterModel filter, int page, int pageSize)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, $"/api/QuanLyLichSuThanhToan/GetDanhSachLichSuThanhToan?page={page}&pageSize={pageSize}");
            req.Content = new StringContent(JsonSerializer.Serialize(filter), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "GetDanhSachLichSuThanhToan");

            var result = new QuanLyLichSuThanhToanPagedResult();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var msg))
                result.Message = msg.GetString();

            if (root.TryGetProperty("data", out var data))
                result.Items = JsonSerializer.Deserialize<List<QuanLyLichSuThanhToanItemDto>>(data.GetRawText(), _jsonOptions) ?? new();

            if (root.TryGetProperty("pagination", out var paging))
                result.Pagination = JsonSerializer.Deserialize<QuanLyLichSuThanhToanPaginationDto>(paging.GetRawText(), _jsonOptions) ?? new();

            return result;
        }

        public async Task<QuanLyLichSuThanhToanDetailDto?> GetChiTietLichSuThanhToanAsync(long id)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, $"/api/QuanLyLichSuThanhToan/GetChiTietLichSuThanhToan?id={id}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "GetChiTietLichSuThanhToan");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<QuanLyLichSuThanhToanDetailDto>>(body, _jsonOptions);
            return wrapper?.Data;
        }

        public Task<List<QuanLyLichSuThanhToanThongKeItemDto>> ThongKeTheoPhuongThucThanhToanAsync(DateTime? fromDate, DateTime? toDate)
            => GetThongKeListAsync($"/api/QuanLyLichSuThanhToan/ThongKeTheoPhuongThucThanhToan{BuildDateQuery(fromDate, toDate)}", "ThongKeTheoPhuongThucThanhToan");

        public Task<List<QuanLyLichSuThanhToanThongKeItemDto>> ThongKeTheoTrangThaiAsync(DateTime? fromDate, DateTime? toDate)
            => GetThongKeListAsync($"/api/QuanLyLichSuThanhToan/ThongKeTheoTrangThai{BuildDateQuery(fromDate, toDate)}", "ThongKeTheoTrangThai");

        public async Task<QuanLyLichSuThanhToanTongQuanDto?> ThongKeTongQuanAsync(DateTime? fromDate, DateTime? toDate)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, $"/api/QuanLyLichSuThanhToan/ThongKeTongQuan{BuildDateQuery(fromDate, toDate)}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "ThongKeTongQuan");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<QuanLyLichSuThanhToanTongQuanDto>>(body, _jsonOptions);
            return wrapper?.Data;
        }

        public async Task<(byte[] Content, string FileName)> ExportExcelAsync(QuanLyLichSuThanhToanFilterModel filter)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyLichSuThanhToan/ExportExcel");
            req.Content = new StringContent(JsonSerializer.Serialize(filter), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var errorBody = await res.Content.ReadAsStringAsync();
                EnsureSuccess(res, errorBody, "ExportExcel");
            }

            var bytes = await res.Content.ReadAsByteArrayAsync();
            var fileName = res.Content.Headers.ContentDisposition?.FileNameStar
                ?? res.Content.Headers.ContentDisposition?.FileName
                ?? $"LichSuThanhToan_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return (bytes, fileName.Trim('"'));
        }

        private async Task<List<QuanLyLichSuThanhToanThongKeItemDto>> GetThongKeListAsync(string path, string apiName)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, path);
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, apiName);

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<QuanLyLichSuThanhToanThongKeItemDto>>>(body, _jsonOptions);
            return wrapper?.Data ?? new();
        }

        private static string BuildDateQuery(DateTime? fromDate, DateTime? toDate)
        {
            var parts = new List<string>();
            if (fromDate.HasValue)
                parts.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("o"))}");
            if (toDate.HasValue)
                parts.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("o"))}");
            return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
        }

        private static void EnsureSuccess(HttpResponseMessage res, string body, string apiName)
        {
            if (res.IsSuccessStatusCode) return;
            throw new Exception($"Loi API {apiName}: {(int)res.StatusCode} - {body}");
        }
    }
}
