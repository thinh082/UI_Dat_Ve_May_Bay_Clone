using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UI_Dat_Ve_May_Bay.Api
{
    public class AuthApi
    {
        private readonly ApiClient _apiClient;

        public AuthApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<(bool ok, string message, string? token, int? loaiTaiKhoan)> LoginAsync(string taiKhoan, string matKhau)
        {
            var url = "/api/XacThucTaiKhoan/dangnhap";

            var payload = new
            {
                TaiKhoan = taiKhoan,
                MatKhau = matKhau
            };

            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                return (false, $"HTTP {(int)res.StatusCode}: {TryReadMessage(json) ?? "Đăng nhập thất bại"}", null, null);
            }

            var token = ExtractToken(json);
            var loaiTaiKhoan = ExtractAccountType(json);
            var msg = TryReadMessage(json) ?? "Đăng nhập thành công";

            if (string.IsNullOrWhiteSpace(token))
                return (false, msg + " (Không tìm thấy token trong response)", null, null);

            return (true, msg, token, loaiTaiKhoan);
        }

        public async Task<(bool ok, string message)> RegisterAsync(object registerBody)
        {
            var url = "/api/XacThucTaiKhoan/DangKy";

            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            req.Content = new StringContent(JsonSerializer.Serialize(registerBody), Encoding.UTF8, "application/json");

            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, $"HTTP {(int)res.StatusCode}: {TryReadMessage(json) ?? "Đăng ký thất bại"}");

            // ✅ FIX: Kiểm tra statusCode trong response body để phát hiện lỗi ngay cả khi HTTP 200
            var msg = TryReadMessage(json) ?? "Đăng ký thành công";
            var statusCode = TryReadStatusCode(json);
            
            // Nếu statusCode trong body >= 400, coi như lỗi
            if (statusCode >= 400)
                return (false, msg);

            return (true, msg);
        }

        public async Task<(bool ok, string message)> ForgotPasswordSendOtpAsync(string email)
        {
            var url = $"/api/XacThucTaiKhoan/QuenMatKhau?email={Uri.EscapeDataString(email)}";

            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, $"HTTP {(int)res.StatusCode}: {TryReadMessage(json) ?? "Gửi OTP thất bại"}");

            // ✅ FIX: Kiểm tra statusCode trong response body
            var msg = TryReadMessage(json) ?? "Đã gửi OTP";
            var statusCode = TryReadStatusCode(json);
            if (statusCode >= 400)
                return (false, msg);

            return (true, msg);
        }

        public async Task<(bool ok, string message)> VerifyOtpAsync(string email, string otp)
        {
            var url =
                $"/api/XacThucTaiKhoan/XacNhanOtp?email={Uri.EscapeDataString(email)}&otp={Uri.EscapeDataString(otp)}";

            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, $"HTTP {(int)res.StatusCode}: {TryReadMessage(json) ?? "Xác nhận OTP thất bại"}");

            // ✅ FIX: Kiểm tra statusCode trong response body
            var msg = TryReadMessage(json) ?? "OTP hợp lệ";
            var statusCode = TryReadStatusCode(json);
            if (statusCode >= 400)
                return (false, msg);

            return (true, msg);
        }

        public async Task<(bool ok, string message)> ResetPasswordAsync(string email, string matKhau, string xacNhanMatKhau)
        {
            var url =
                $"/api/XacThucTaiKhoan/DoiMatKhau?email={Uri.EscapeDataString(email)}&matKhau={Uri.EscapeDataString(matKhau)}&xacNhanMatKhau={Uri.EscapeDataString(xacNhanMatKhau)}";

            using var req = _apiClient.CreateRequest(HttpMethod.Post, url);
            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, $"HTTP {(int)res.StatusCode}: {TryReadMessage(json) ?? "Đổi mật khẩu thất bại"}");

            // ✅ FIX: Kiểm tra statusCode trong response body
            var msg = TryReadMessage(json) ?? "Đổi mật khẩu thành công";
            var statusCode = TryReadStatusCode(json);
            if (statusCode >= 400)
                return (false, msg);

            return (true, msg);
        }

        // ==========================
        // SOCIAL LOGIN: lấy URL OAuth
        // ==========================

        public Task<(bool ok, string message, string? url)> GetGoogleLoginUrlAsync(string state)
            => GetSocialUrlAsync("/api/XacThucTaiKhoan/Create_url_google", state);

        public Task<(bool ok, string message, string? url)> GetFacebookLoginUrlAsync(string state)
            => GetSocialUrlAsync("/api/XacThucTaiKhoan/Create_url_facebook", state);

        public Task<(bool ok, string message, string? url)> GetZaloLoginUrlAsync(string state)
            => GetSocialUrlAsync("/api/XacThucTaiKhoan/Create_url_zalo", state);

        private async Task<(bool ok, string message, string? url)> GetSocialUrlAsync(string endpoint, string state)
        {
            var url = $"{endpoint}?state={Uri.EscapeDataString(state)}";

            using var req = _apiClient.CreateRequest(HttpMethod.Get, url);
            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, $"HTTP {(int)res.StatusCode}: {TryReadMessage(json) ?? "Không lấy được url social"}", null);

            var socialUrl = TryReadUrl(json);
            var msg = TryReadMessage(json) ?? "OK";

            if (string.IsNullOrWhiteSpace(socialUrl))
                return (false, msg + " (Không tìm thấy url trong response)", null);

            return (true, msg, socialUrl);
        }

        // -------- Helpers --------

        private static string? TryReadUrl(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("url", out var u)) return u.GetString();
                    if (root.TryGetProperty("Url", out var u2)) return u2.GetString();
                }
            }
            catch { }
            return null;
        }

        private static string? TryReadMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var msg)) return msg.GetString();
                    if (root.TryGetProperty("Message", out var msg2)) return msg2.GetString();

                    if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                    {
                        if (data.TryGetProperty("message", out var dmsg)) return dmsg.GetString();
                    }
                }
            }
            catch { }
            return null;
        }

        // ✅ FIX: Helper để đọc statusCode từ response body
        private static int TryReadStatusCode(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("statusCode", out var sc) && sc.ValueKind == JsonValueKind.Number)
                        return sc.GetInt32();
                    if (root.TryGetProperty("StatusCode", out var sc2) && sc2.ValueKind == JsonValueKind.Number)
                        return sc2.GetInt32();
                }
            }
            catch { }
            return 200; // Mặc định là thành công nếu không tìm thấy
        }

        private static string? ExtractToken(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("token", out var t)) return t.GetString();
                    if (root.TryGetProperty("Token", out var t2)) return t2.GetString();
                    if (root.TryGetProperty("accessToken", out var t3)) return t3.GetString();
                    if (root.TryGetProperty("jwt", out var t4)) return t4.GetString();

                    if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                    {
                        if (data.TryGetProperty("token", out var dt)) return dt.GetString();
                        if (data.TryGetProperty("Token", out var dt2)) return dt2.GetString();
                        if (data.TryGetProperty("accessToken", out var dt3)) return dt3.GetString();
                    }
                }
            }
            catch { }

            return null;
        }

        private static int? ExtractAccountType(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("loaiTaiKhoan", out var l) && l.ValueKind == JsonValueKind.Number) return l.GetInt32();
                    if (root.TryGetProperty("LoaiTaiKhoan", out var l2) && l2.ValueKind == JsonValueKind.Number) return l2.GetInt32();

                    if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                    {
                        if (data.TryGetProperty("loaiTaiKhoan", out var dl) && dl.ValueKind == JsonValueKind.Number) return dl.GetInt32();
                        if (data.TryGetProperty("LoaiTaiKhoan", out var dl2) && dl2.ValueKind == JsonValueKind.Number) return dl2.GetInt32();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
