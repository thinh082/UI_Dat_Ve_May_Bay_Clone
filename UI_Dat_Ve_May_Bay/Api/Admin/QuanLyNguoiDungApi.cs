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
    public class QuanLyNguoiDungApi
    {
        private readonly ApiClient _apiClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public QuanLyNguoiDungApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<QuanLyNguoiDungItemDto>> GetDanhSachNguoiDungAsync(QuanLyNguoiDungFilterModel filter)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyNguoiDung/GetDanhSachNguoiDung");
            req.Content = new StringContent(JsonSerializer.Serialize(filter), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "GetDanhSachNguoiDung");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<QuanLyNguoiDungItemDto>>>(body, _jsonOptions)
                ?? new ApiResponse<List<QuanLyNguoiDungItemDto>>();

            EnsureApiSuccess(wrapper, "Lay danh sach nguoi dung that bai.");
            return wrapper.Data ?? new List<QuanLyNguoiDungItemDto>();
        }

        public async Task<QuanLyNguoiDungDetailDto?> GetChiTietNguoiDungAsync(long idTaiKhoan)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, $"/api/QuanLyNguoiDung/GetChiTietNguoiDung?idTaiKhoan={idTaiKhoan}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "GetChiTietNguoiDung");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<QuanLyNguoiDungDetailDto>>(body, _jsonOptions)
                ?? new ApiResponse<QuanLyNguoiDungDetailDto>();

            EnsureApiSuccess(wrapper, "Lay chi tiet nguoi dung that bai.");
            return wrapper.Data;
        }

        public async Task<ApiResponse<object>> CapNhatNguoiDungAsync(QuanLyNguoiDungUpdateModel model)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "/api/QuanLyNguoiDung/CapNhatNguoiDung");
            req.Content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "CapNhatNguoiDung");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object>();

            EnsureApiSuccess(wrapper, "Cap nhat nguoi dung that bai.");
            return wrapper;
        }

        public async Task<ApiResponse<object>> XoaNguoiDungAsync(long idTaiKhoan)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, $"/api/QuanLyNguoiDung/XoaNguoiDung?idTaiKhoan={idTaiKhoan}");
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            EnsureSuccess(res, body, "XoaNguoiDung");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions)
                ?? new ApiResponse<object>();

            EnsureApiSuccess(wrapper, "Xoa nguoi dung that bai.");
            return wrapper;
        }

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
