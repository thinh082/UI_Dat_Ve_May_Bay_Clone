using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UI_Dat_Ve_May_Bay.Api
{
    public class FlightApi
    {
        private readonly ApiClient _api;

        public FlightApi(ApiClient apiClient)
        {
            _api = apiClient;
        }

        /// <summary>
        /// BE: POST /api/ChuyenBay/LayDanhSachChuyenBay
        /// Body: (idLoaiVe, idHangBay, maSanBayDi, maSanBayDen, ngayDi "dd-MM-yyyy", giaMin, giaMax, idTienNghi STRING)
        /// </summary>
        public async Task<JsonElement?> SearchFlightsAsync(object req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            var url = "/api/ChuyenBay/LayDanhSachChuyenBay";

            // ---- lấy input từ req (hỗ trợ camelCase/PascalCase) ----
            var maSanBayDi = GetString(req, "maSanBayDi", "MaSanBayDi")?.Trim().ToUpperInvariant() ?? "";
            var maSanBayDen = GetString(req, "maSanBayDen", "MaSanBayDen")?.Trim().ToUpperInvariant() ?? "";
            var ngayDiStr = ToDdMMyyyy(GetObj(req, "ngayDi", "NgayDi"));

            var giaMin = GetNullableDecimal(req, "giaMin", "GiaMin") ?? 0;
            var giaMax = GetNullableDecimal(req, "giaMax", "GiaMax") ?? 0;

            // BE hay require 2 cái này (log OK khi gửi = 1)
            var idLoaiVe = GetNullableInt(req, "idLoaiVe", "IdLoaiVe") ?? 1;
            var idHangBay = GetNullableInt(req, "idHangBay", "IdHangBay") ?? 1;

            // BE require STRING
            var idTienNghi = ToStringForBe(GetObj(req, "idTienNghi", "IdTienNghi"));
            if (string.IsNullOrWhiteSpace(idTienNghi)) idTienNghi = "1";

            // validate nhẹ
            if (string.IsNullOrWhiteSpace(maSanBayDi) || string.IsNullOrWhiteSpace(maSanBayDen))
                throw new Exception("Thiếu MaSanBayDi hoặc MaSanBayDen.");
            if (string.IsNullOrWhiteSpace(ngayDiStr))
                throw new Exception($"NgayDi invalid: '{GetObj(req, "ngayDi", "NgayDi")}'");

            // ✅ Payload đúng format log đã chạy OK: camelCase + idTienNghi string
            var payload = new
            {
                idLoaiVe = idLoaiVe,
                idHangBay = idHangBay,
                maSanBayDi = maSanBayDi,
                maSanBayDen = maSanBayDen,
                ngayDi = ngayDiStr,   // "dd-MM-yyyy"
                giaMin = giaMin,
                giaMax = giaMax,
                idTienNghi = idTienNghi
            };

            // ---- log file (Desktop) ----
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "flight_search.log"
            );

            // Gửi body (application/json) — KHÔNG gửi query (query đã bị 415 trong log)
            var (okA, jsonA, statusA) = await PostJson(url, payload, logPath, "TRY_BODY");
            if (okA)
                return ExtractData(jsonA);

            // Nếu BE báo model required (tùy action signature) thì bọc wrapper
            if (statusA == HttpStatusCode.BadRequest && LooksLikeModelRequired(jsonA))
            {
                var payloadB = new { model = payload };
                var (okB, jsonB, statusB) = await PostJson(url, payloadB, logPath, "TRY_BODY_MODEL_WRAPPER");

                if (okB)
                    return ExtractData(jsonB);

                throw new Exception($"SearchFlights failed (retry wrapper): {(int)statusB} {statusB} | {jsonB}");
            }

            throw new Exception($"SearchFlights failed: {(int)statusA} {statusA} | {jsonA}");
        }

        // ================= helpers =================

        private async Task<(bool ok, string json, HttpStatusCode status)> PostJson(string url, object payload, string logPath, string tag)
        {
            var http = _api.Http;

            // ✅ Search flight thường không cần auth → để false cho an toàn (tránh 401 do token rỗng)
            var request = _api.CreateRequest(HttpMethod.Post, url, false);

            var body = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false
            });

            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {tag} REQUEST {url}\n{body}\n");

            var resp = await http.SendAsync(request);
            var json = await resp.Content.ReadAsStringAsync();

            File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {tag} RESPONSE {(int)resp.StatusCode} {resp.ReasonPhrase}\n{json}\n-----------------\n");

            return (resp.IsSuccessStatusCode, json, resp.StatusCode);
        }

        // ✅ FIX: Extract "data"/"Data"/"DATA" case-insensitive
        private static JsonElement? ExtractData(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                // scan properties case-insensitively for "data"
                foreach (var p in root.EnumerateObject())
                {
                    if (string.Equals(p.Name, "data", StringComparison.OrdinalIgnoreCase))
                        return p.Value.Clone();
                }

                return root.Clone();
            }

            return root.Clone();
        }

        private static bool LooksLikeModelRequired(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            return json.Contains("\"errors\"", StringComparison.OrdinalIgnoreCase)
                && json.Contains("\"model\"", StringComparison.OrdinalIgnoreCase)
                && json.Contains("required", StringComparison.OrdinalIgnoreCase);
        }

        private static object? GetObj(object req, params string[] names)
        {
            foreach (var n in names)
            {
                var p = req.GetType().GetProperty(n);
                if (p == null) continue;
                return p.GetValue(req);
            }
            return null;
        }

        private static string? GetString(object req, params string[] names)
        {
            var v = GetObj(req, names);
            return v?.ToString();
        }

        private static int? GetNullableInt(object req, params string[] names)
        {
            var v = GetObj(req, names);
            if (v == null) return null;
            if (v is int i) return i;
            if (int.TryParse(v.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static decimal? GetNullableDecimal(object req, params string[] names)
        {
            var v = GetObj(req, names);
            if (v == null) return null;
            if (v is decimal d) return d;
            if (v is double db) return (decimal)db;
            if (v is float f) return (decimal)f;

            if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
                return parsed;

            return null;
        }

        private static string ToDdMMyyyy(object? ngayDiObj)
        {
            if (ngayDiObj == null) return "";

            if (ngayDiObj is DateTime dt)
                return dt.ToString("dd-MM-yyyy");

            var s = ngayDiObj.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(s)) return "";

            if (DateTime.TryParse(s, out var parsed))
                return parsed.ToString("dd-MM-yyyy");

            string[] fmts = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(s, fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed.ToString("dd-MM-yyyy");

            return "";
        }

        /// <summary>
        /// Ép IdTienNghi về string đúng như BE yêu cầu.
        /// - null => ""
        /// - int/long/... => "15"
        /// - list/array => "1,2,3"
        /// - string => giữ nguyên
        /// </summary>
        private static string ToStringForBe(object? value)
        {
            if (value == null) return "";

            if (value is string s) return s.Trim();

            if (value is IEnumerable enumerable && value is not string)
            {
                var parts = enumerable.Cast<object?>()
                    .Where(x => x != null)
                    .Select(x => x!.ToString()!.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                return string.Join(",", parts);
            }

            return value.ToString()?.Trim() ?? "";
        }
    }
}