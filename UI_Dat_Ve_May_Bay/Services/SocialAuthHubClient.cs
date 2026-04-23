using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace UI_Dat_Ve_May_Bay.Services
{
    public class SocialAuthHubClient
    {
        private HubConnection? _conn;

        public async Task ConnectAsync(string hubUrl)
        {
            _conn = new HubConnectionBuilder()
                .WithUrl(hubUrl) // https://audrina-subultimate-ghostily.ngrok-free.dev/hubs/notify
                .WithAutomaticReconnect()
                .Build();

            await _conn.StartAsync();
        }

        public void OnGoogleResult(Action<string?> onToken)
        {
            _conn?.On<object>("ReceiveGoogleAuthResult", payload =>
            {
                var token = ExtractJwtFromPayload(payload);
                onToken(token);
            });
        }

        public void OnFacebookResult(Action<string?> onToken)
        {
            _conn?.On<object>("ReceiveFacebookAuthResult", payload =>
            {
                var token = ExtractJwtFromPayload(payload);
                onToken(token);
            });
        }

        public void OnZaloResult(Action<string?> onToken)
        {
            _conn?.On<object>("ReceiveZaloAuthResult", payload =>
            {
                var token = ExtractJwtFromPayload(payload);
                onToken(token);
            });
        }

        // ✅ QUAN TRỌNG: tên method JoinGroup phải đúng với NotificationHub.cs
        public Task JoinGroupAsync(string state)
            => _conn!.InvokeAsync("JoinGroup", state);

        public async Task DisconnectAsync()
        {
            if (_conn != null)
                await _conn.StopAsync();
        }

        private static string? ExtractJwtFromPayload(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                using var doc = JsonDocument.Parse(json);

                // controller gửi: new { result } => thường payload có "result"
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("result", out var result))
                    {
                        // token có thể nằm trong result.token hoặc result.data.token...
                        if (result.ValueKind == JsonValueKind.Object)
                        {
                            if (result.TryGetProperty("token", out var t)) return t.GetString();
                            if (result.TryGetProperty("Token", out var t2)) return t2.GetString();

                            if (result.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                            {
                                if (data.TryGetProperty("token", out var dt)) return dt.GetString();
                                if (data.TryGetProperty("Token", out var dt2)) return dt2.GetString();
                            }
                        }
                    }

                    // fallback nếu token ở root
                    if (root.TryGetProperty("token", out var t3)) return t3.GetString();
                }
            }
            catch { }
            return null;
        }
    }
}
