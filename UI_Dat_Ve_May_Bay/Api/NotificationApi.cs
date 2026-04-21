using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Models.Notifications;

namespace UI_Dat_Ve_May_Bay.Api
{
    public class NotificationApi
    {
        private readonly ApiClient _apiClient;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public NotificationApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // POST: api/ThongBao/ThongBao  -> trả về List<NotificationDto>
        public async Task<List<NotificationDto>> GetThongBaoAsync()
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Post, "api/ThongBao/ThongBao");
            req.Content = new StringContent("", Encoding.UTF8, "application/json");

            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API ThongBao: {(int)res.StatusCode} - {body}");

            var data = JsonSerializer.Deserialize<List<NotificationDto>>(body, _jsonOptions);
            return data ?? new List<NotificationDto>();
        }

        // POST: api/ThongBao/ChiTietThongBao?idThongBao=123
        // BE trả về wrapper { statusCode, message, data: {...} }
        public async Task<NotificationDto> GetChiTietThongBaoAsync(long idThongBao)
        {
            var url = $"api/ThongBao/ChiTietThongBao?idThongBao={idThongBao}";
            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            req.Content = new StringContent("", Encoding.UTF8, "application/json");

            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API ChiTietThongBao: {(int)res.StatusCode} - {body}");

            // unwrap ApiResponse<NotificationDto>
            var wrapper = JsonSerializer.Deserialize<ApiResponse<NotificationDto>>(body, _jsonOptions);

            if (wrapper == null)
                throw new Exception("Không đọc được dữ liệu chi tiết.");

            if (wrapper.StatusCode != 200 || wrapper.Data == null)
                throw new Exception(wrapper.Message ?? "Không lấy được chi tiết thông báo.");

            return wrapper.Data;
        }

        // POST: api/ThongBao/XoaThongBao?idThongBao=123
        public async Task XoaThongBaoAsync(long idThongBao)
        {
            var url = $"api/ThongBao/XoaThongBao?idThongBao={idThongBao}";
            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            req.Content = new StringContent("", Encoding.UTF8, "application/json");

            var res = await _apiClient.Http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Lỗi API XoaThongBao: {(int)res.StatusCode} - {body}");
        }
    }
}
