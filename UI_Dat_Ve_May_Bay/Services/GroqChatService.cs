using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UI_Dat_Ve_May_Bay.Services
{
    public class GroqChatService
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient();
        private readonly string _apiKey = "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";
        private readonly string _baseUrl = "https://openrouter.ai/api/v1";
        private readonly string _model = "openrouter/free";

        public GroqChatService()
        {
        }

        public async Task<(bool ok, string? reply, string? error)> GetReplyAsync(List<GroqChatTurn> conversation)
        {
            try
            {
                var payload = new
                {
                    model = _model,
                    messages = conversation
                };

                var url = $"{_baseUrl}/chat/completions";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await SharedHttpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = TryReadError(json) ?? $"HTTP {(int)response.StatusCode}";
                    return (false, null, error);
                }

                var reply = TryReadReply(json);
                return string.IsNullOrWhiteSpace(reply) 
                    ? (false, null, "No response content") 
                    : (true, reply.Trim(), null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        private static string? TryReadReply(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) &&
                    choices.ValueKind == JsonValueKind.Array &&
                    choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        return content.GetString();
                    }
                }
            }
            catch { }

            return null;
        }

        private static string? TryReadError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var message))
                        return message.GetString();
                }
            }
            catch { }

            return null;
        }
    }

    public class GroqChatTurn
    {
        public GroqChatTurn(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        public string role { get; set; }
        public string content { get; set; }
    }
}
