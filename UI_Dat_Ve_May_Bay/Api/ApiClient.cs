// Api/ApiClient.cs
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UI_Dat_Ve_May_Bay.Api
{
    public class ApiClient
    {
        public HttpClient Http { get; }

        // ✅ cho phép set (đỡ lỗi "BaseUrl read only")//
        public string BaseUrl { get; set; }

        public string? Token { get; set; }

        // ✅ ctor rỗng để XAML/VM new ApiClient() không lỗi
        public ApiClient() : this("https://audrina-subultimate-ghostily.ngrok-free.dev") { }

        // ✅ ctor theo baseUrl (đỡ lỗi "required parameter baseUrl")
        public ApiClient(string baseUrl)
        {
            BaseUrl = baseUrl.TrimEnd('/');
            Http = new HttpClient();
        }

        // ✅ tương thích code cũ gọi ApplyBaseUrl()
        public void ApplyBaseUrl(string baseUrl)
        {
            BaseUrl = (baseUrl ?? "").TrimEnd('/');
        }

        // ✅ overload không tham số (giữ tương thích code cũ đã gọi ApplyBaseUrl();)
        public void ApplyBaseUrl()
        {
            BaseUrl = (BaseUrl ?? "").TrimEnd('/');
        }

        // ✅ thêm attachAuth để khỏi lỗi CS1739 (named parameter 'attachAuth')
        public HttpRequestMessage CreateRequest(HttpMethod method, string path, bool attachAuth = true)
        {
            if (string.IsNullOrWhiteSpace(path)) path = "/";
            if (!path.StartsWith("/")) path = "/" + path;

            var req = new HttpRequestMessage(method, BaseUrl + path);

            if (attachAuth && !string.IsNullOrWhiteSpace(Token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

            return req;
        }

        public HttpRequestMessage CreateJsonRequest<T>(HttpMethod method, string path, T body, bool attachAuth = true)
        {
            var req = CreateRequest(method, path, attachAuth);
            var json = JsonSerializer.Serialize(body);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return req;
        }

        public string GetUserIdFromToken()
        {
            if (string.IsNullOrWhiteSpace(Token)) return "anonymous";
            try
            {
                var parts = Token.Split('.');
                if (parts.Length < 2) return "anonymous";
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                while (payload.Length % 4 != 0) payload += "=";
                var bytes = Convert.FromBase64String(payload);
                var json = Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Priority list of claims (Common .NET schemas first for stability)
                string[] claimNames = { 
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
                    "sub", 
                    "email", 
                    "unique_name", 
                    "nameid",
                    "name",
                    "Preferred_username"
                };

                foreach (var name in claimNames)
                {
                    if (root.TryGetProperty(name, out var prop))
                    {
                        var val = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) return val;
                    }
                }

                // Fallback: search all properties for something that looks like an ID/Email
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name.Contains("name", StringComparison.OrdinalIgnoreCase) || 
                        prop.Name.Contains("id", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Contains("email", StringComparison.OrdinalIgnoreCase))
                    {
                        var val = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) return val;
                    }
                }
            }
            catch { }
            return "anonymous";
        }
    }
}