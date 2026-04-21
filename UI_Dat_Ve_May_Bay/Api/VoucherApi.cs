using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Models.Common;
using UI_Dat_Ve_May_Bay.Models.Vouchers;

namespace UI_Dat_Ve_May_Bay.Api
{
    public class VoucherApi
    {
        private readonly ApiClient _apiClient;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public VoucherApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Voucher của tài khoản (cần token vì BE đọc claim NameIdentifier)
        public async Task<List<VoucherDto>> LayToanBoPhieuGiamGiaAsync()
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, "api/PhieuGiamGia/LayToanBoPhieuGiamGia");
            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API LayToanBoPhieuGiamGia: {(int)res.StatusCode} - {body}");

            return ParseListOrWrapper(body);
        }

        // Danh sách voucher toàn hệ thống (JSON bạn đưa là wrapper statusCode/message/data)
        public async Task<List<VoucherDto>> GetDanhSachPhieuGiamGiaAsync()
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, "api/PhieuGiamGia/GetDanhSachPhieuGiamGia");
            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API GetDanhSachPhieuGiamGia: {(int)res.StatusCode} - {body}");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<List<VoucherDto>>>(body, _jsonOptions);
            return wrapper?.Data ?? new List<VoucherDto>();
        }

        public async Task<List<VoucherDto>> TimKiemMaGiamGiaAsync(string maGiamGia)
        {
            var url = $"api/PhieuGiamGia/TimKiemMaGiamGia?maGiamGia={Uri.EscapeDataString(maGiamGia ?? "")}";
            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            req.Content = new StringContent(""); // body rỗng
            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API TimKiemMaGiamGia: {(int)res.StatusCode} - {body}");

            return ParseListOrWrapper(body);
        }

        public async Task<ApiResponse<object>> ApplyVoucherAsync(long idMaGiamGia)
        {
            var url = $"api/PhieuGiamGia/ApplyVoucher?idMaGiamGia={idMaGiamGia}";
            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            req.Content = new StringContent(""); // body rỗng
            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API ApplyVoucher: {(int)res.StatusCode} - {body}");

            var wrapper = JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions);
            return wrapper ?? new ApiResponse<object> { StatusCode = 200, Message = "OK", Data = null };
        }

        public async Task<List<VoucherDto>> LayDanhSachChiTietPhieuGiamGiaAsync()
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "api/PhieuGiamGia/LayDanhSachChiTietPhieuGiamGia");
            req.Content = new StringContent(""); // body rỗng
            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API LayDanhSachChiTietPhieuGiamGia: {(int)res.StatusCode} - {body}");

            return ParseListOrWrapper(body);
        }

        private List<VoucherDto> ParseListOrWrapper(string json)
        {
            // 1) thử parse list trực tiếp
            try
            {
                var list = JsonSerializer.Deserialize<List<VoucherDto>>(json, _jsonOptions);
                if (list != null) return list;
            }
            catch { }

            // 2) thử parse wrapper
            try
            {
                var wrapper = JsonSerializer.Deserialize<ApiResponse<List<VoucherDto>>>(json, _jsonOptions);
                if (wrapper?.Data != null) return wrapper.Data;
            }
            catch { }

            return new List<VoucherDto>();
        }
    }
}
