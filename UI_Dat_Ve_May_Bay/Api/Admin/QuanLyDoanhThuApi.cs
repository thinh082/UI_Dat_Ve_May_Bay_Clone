using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Models.Admin;

namespace UI_Dat_Ve_May_Bay.Api.Admin
{
    public class QuanLyDoanhThuApi
    {
        private readonly ApiClient _apiClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public QuanLyDoanhThuApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<QuanLyDoanhThuResultDto> DoanhThuTheoNgayAsync(string ngay)
            => GetResultAsync($"/api/QuanLyDoanhThu/DoanhThuTheoNgay?ngay={Uri.EscapeDataString(ngay)}", "DoanhThuTheoNgay");

        public Task<QuanLyDoanhThuResultDto> DoanhThuTheoThangAsync(string thang)
            => GetResultAsync($"/api/QuanLyDoanhThu/DoanhThuTheoThang?thang={Uri.EscapeDataString(thang)}", "DoanhThuTheoThang");

        public Task<QuanLyDoanhThuResultDto> DoanhThuTheoNamAsync(string nam)
            => GetResultAsync($"/api/QuanLyDoanhThu/DoanhThuTheoNam?nam={Uri.EscapeDataString(nam)}", "DoanhThuTheoNam");

        private async Task<QuanLyDoanhThuResultDto> GetResultAsync(string path, string apiName)
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, path);
            using var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Loi API {apiName}: {(int)res.StatusCode} - {body}");

            return JsonSerializer.Deserialize<QuanLyDoanhThuResultDto>(body, _jsonOptions)
                ?? new QuanLyDoanhThuResultDto { StatusCode = 200, Message = "OK" };
        }
    }
}
