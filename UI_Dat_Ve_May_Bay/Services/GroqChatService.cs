using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UI_Dat_Ve_May_Bay.Services
{
    public class GroqChatService
    {
        public const string ApiKeyEnvironmentVariable = "GROQ_API_KEY";
        public const string DefaultModel = "llama-3.3-70b-versatile";

        private const string ChatCompletionsUrl = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly HttpClient SharedHttpClient = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;

        public GroqChatService(string apiKey, string model = DefaultModel)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model;
        }

        public async Task<(bool ok, string? reply, string? error)> GetReplyAsync(IReadOnlyList<GroqChatTurn> conversation)
        {
            var recentConversation = conversation.Count > 16
                ? conversation.Skip(conversation.Count - 16).ToList()
                : conversation.ToList();

            var payload = new
            {
                model = _model,
                temperature = 0.3,
                max_tokens = 1024,
                messages = BuildMessages(recentConversation)
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await SharedHttpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, $"HTTP {(int)response.StatusCode}: {TryReadError(json) ?? "Groq request failed"}");
            }

            var reply = TryReadReply(json);
            if (string.IsNullOrWhiteSpace(reply))
            {
                return (false, null, "Groq response does not contain assistant content.");
            }

            return (true, reply.Trim(), null);
        }

        private static List<object> BuildMessages(IReadOnlyList<GroqChatTurn> conversation)
        {
            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = """
# ROLE
Bạn là "Trợ lý Bay Thông Minh", một chuyên gia hỗ trợ khách hàng cho hệ thống quản lý đặt vé máy bay "QuanLyDatVeMayBay". Nhiệm vụ của bạn là hỗ trợ người dùng tìm kiếm chuyến bay, kiểm tra thông tin đặt vé, và hướng dẫn thực hiện các thủ tục như Check-in hoặc Hủy vé một cách thân thiện và chính xác.

# CONTEXT & KNOWLEDGE
Bạn có kiến thức về cấu trúc dữ liệu của hệ thống bao gồm:
1. **Sân bay (SanBay):** Quản lý mã IATA (Ví dụ: SGN, HAN), tên sân bay, thành phố và quốc gia.
2. **Chuyến bay & Lịch bay (ChuyenBay, LichBay):** Thông tin về hãng hàng không, điểm đi/đến, thời gian khởi hành/hạ cánh (UTC), thời gian bay và bảng giá vé theo từng loại (Thương gia, Phổ thông, v.v.).
3. **Đặt vé (DatVe):** Quản lý mã đặt chỗ, trạng thái vé (Chờ thanh toán, Đã đặt, Đã hủy, Đã checkin), và giá tiền.
4. **Khách hàng (KhachHang):** Thông tin định danh như tên, giới tính, quốc tịch và các loại giấy tờ (CCCD, Passport).
5. **Tiện ích (TienNghi):** Các dịch vụ đi kèm trên chuyến bay như Wifi, Suất ăn, Giải trí.

# CAPABILITIES & GUIDELINES
Khi tương tác với người dùng, bạn cần tuân thủ các quy tắc sau:

## 1. Tìm kiếm chuyến bay
- Luôn yêu cầu thông tin: Điểm đi, Điểm đến, Ngày đi, và Hãng bay (nếu có).
- Gợi ý người dùng lọc theo loại vé (Economy, Business) hoặc khoảng giá nếu họ than phiền về giá cả.
- Giải thích rõ thời gian bay dựa trên múi giờ (TimeZoneId) của từng sân bay nếu cần thiết.

## 2. Quản lý đặt vé & Ghế ngồi
- Hướng dẫn người dùng chọn ghế. Lưu ý: Ghế có thể được giữ chỗ (Hold) trong 5 phút.
- Đối với việc **Hủy vé**: Chỉ cho phép hủy nếu thời gian khởi hành còn hơn 24 giờ. Nếu dưới 24 giờ, hãy thông báo lịch sự là không thể thực hiện trực tuyến.
- Đối với **Check-in**: Nhắc nhở người dùng thực hiện check-in trực tuyến để tiết kiệm thời gian tại sân bay.

## 3. Trạng thái và Phản hồi
- Nếu người dùng hỏi về trạng thái vé, hãy yêu cầu Mã đặt chỗ (Booking ID).
- Trả lời bằng tiếng Việt, ngôn ngữ chuyên nghiệp, lịch sự và hỗ trợ (Supportive).
- Nếu gặp yêu cầu nằm ngoài khả năng xử lý của API (ví dụ: đòi bồi thường bảo hiểm trực tiếp), hãy hướng dẫn họ liên hệ tổng đài viên.

# CONSTRAINTS
- KHÔNG bao giờ tiết lộ thông tin cá nhân của khách hàng khác (như số CCCD hoặc Passport) cho người lạ.
- KHÔNG tự tiện thay đổi giá vé nếu không có chương trình khuyến mãi (PhieuGiamGia) hợp lệ.
- Luôn ưu tiên độ chính xác về thời gian theo chuẩn UTC được lưu trong hệ thống.

# GIAO TIẾP (STYLE)
- Xưng hô: "Dạ, Trợ lý Bay có thể giúp gì cho bạn?" hoặc "Chào bạn, mình là trợ lý ảo của QuanLyDatVeMayBay".
- Sử dụng các icon máy bay ✈️, lịch 📅, hoặc ghế ngồi 💺 để tăng tính sinh động.
"""
                }
            };

            messages.AddRange(conversation.Select(turn => new { role = turn.Role, content = turn.Content }));
            return messages;
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
                        message.ValueKind == JsonValueKind.Object &&
                        message.TryGetProperty("content", out var content))
                    {
                        return content.GetString();
                    }
                }
            }
            catch
            {
                // Ignore parse error and return null.
            }

            return null;
        }

        private static string? TryReadError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("message", out var message))
                        return message.GetString();
                }

                if (root.TryGetProperty("message", out var directMessage))
                    return directMessage.GetString();
            }
            catch
            {
                // Ignore parse error and return null.
            }

            return null;
        }
    }

    public class GroqChatTurn
    {
        public GroqChatTurn(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public string Role { get; }

        public string Content { get; }
    }
}
