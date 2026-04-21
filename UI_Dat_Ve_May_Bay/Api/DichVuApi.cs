using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace UI_Dat_Ve_May_Bay.Api
{
    public class DichVuApi
    {
        private readonly ApiClient _apiClient;

        public DichVuApi(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<(List<VeMayBayItemDto> items, string message)> LayDanhSachVeMayBayAsync()
        {
            using var req = _apiClient.CreateRequest(HttpMethod.Get, "api/DichVu/LayDanhSachDichVu");
            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception(BuildHttpError(res, json));

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = doc.RootElement;

            EnsureBusinessSuccess(root);

            var message = GetString(root, "message", "Message") ?? "OK";
            if (!TryGetPropertyCI(root, "data", out var data) || data.ValueKind != JsonValueKind.Object)
                return (new List<VeMayBayItemDto>(), message);

            if (!TryGetPropertyCI(data, "VeMayBay", out var veMayBay) || veMayBay.ValueKind != JsonValueKind.Array)
                return (new List<VeMayBayItemDto>(), message);

            var list = new List<VeMayBayItemDto>();
            foreach (var item in veMayBay.EnumerateArray())
            {
                list.Add(new VeMayBayItemDto
                {
                    Id = GetLong(item, "id", "Id"),
                    IdLichBay = GetLong(item, "idLichBay", "IdLichBay"),
                    NgayDat = GetDateTime(item, "ngayDat", "NgayDat"),
                    DiemDi = GetString(item, "diemDi", "DiemDi") ?? "",
                    DiemDen = GetString(item, "diemDen", "DiemDen") ?? "",
                    ThoiGianBatDau = GetDateTime(item, "thoiGianBatDau", "ThoiGianBatDau"),
                    ThoiGianKetThuc = GetDateTime(item, "thoiGianKetThuc", "ThoiGianKetThuc"),
                    TrangThaiRaw = GetRawText(item, "trangThai", "TrangThai")
                });
            }

            return (list.OrderByDescending(x => x.NgayDat).ToList(), message);
        }

        public async Task<(VeMayBayDetailDto? detail, string message)> LayChiTietVeMayBayAsync(long idDichVu)
        {
            var path = $"api/DichVu/ChiTietDichVu?idDichVu={idDichVu}&loaiDichVu=1";
            using var req = _apiClient.CreateRequest(HttpMethod.Get, path);
            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception(BuildHttpError(res, json));

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = doc.RootElement;
            EnsureBusinessSuccess(root);

            var message = GetString(root, "message", "Message") ?? "OK";
            if (!TryGetPropertyCI(root, "data", out var data) || data.ValueKind != JsonValueKind.Object)
                return (null, message);

            var detail = new VeMayBayDetailDto
            {
                Id = GetLong(data, "id", "Id"),
                NgayDat = GetDateTime(data, "ngayDat", "NgayDat"),
                DiemDi = GetString(data, "diemDi", "DiemDi") ?? "",
                DiemDen = GetString(data, "diemDen", "DiemDen") ?? "",
                MaSanBayDi = GetString(data, "maSanBayDi", "MaSanBayDi") ?? "",
                MaSanBayDen = GetString(data, "maSanBayDen", "MaSanBayDen") ?? "",
                ThoiGianBatDau = GetDateTime(data, "thoiGianBatDau", "ThoiGianBatDau"),
                ThoiGianKetThuc = GetDateTime(data, "thoiGianKetThuc", "ThoiGianKetThuc"),
            };

            return (detail, message);
        }

        public async Task<string> HuyVeMayBayAsync(long idDatVe, string lyDoHuy)
        {
            var reason = string.IsNullOrWhiteSpace(lyDoHuy) ? "Hủy từ màn Chuyến bay của bạn" : lyDoHuy.Trim();
            var path = $"api/ChuyenBay/HuyDatVe?idDatVe={idDatVe}&lyDoHuy={Uri.EscapeDataString(reason)}";

            using var req = _apiClient.CreateRequest(HttpMethod.Post, path);
            req.Content = new StringContent("");

            using var res = await _apiClient.Http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception(BuildHttpError(res, json));

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = doc.RootElement;
            EnsureBusinessSuccess(root);

            return GetString(root, "message", "Message") ?? "Đã gửi yêu cầu hủy vé thành công.";
        }

        private static void EnsureBusinessSuccess(JsonElement root)
        {
            if (!TryGetPropertyCI(root, "statusCode", out var statusCode))
                return;

            var isError = statusCode.ValueKind switch
            {
                JsonValueKind.Number => statusCode.TryGetInt32(out var n) && n >= 400,
                JsonValueKind.False => true,
                JsonValueKind.True => false,
                JsonValueKind.String => ParseStatusCodeString(statusCode.GetString()),
                _ => false
            };

            if (!isError) return;

            var message = GetString(root, "message", "Message") ?? "Backend trả về lỗi nghiệp vụ.";
            throw new Exception(message);
        }

        private static bool ParseStatusCodeString(string? raw)
        {
            var value = (raw ?? "").Trim();
            if (int.TryParse(value, out var n)) return n >= 400;
            if (bool.TryParse(value, out var b)) return !b;
            return false;
        }

        private static string BuildHttpError(HttpResponseMessage response, string body)
        {
            var msg = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
            if (string.IsNullOrWhiteSpace(body)) return msg;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var parsed = GetString(doc.RootElement, "message", "Message");
                if (!string.IsNullOrWhiteSpace(parsed)) return $"{msg}: {parsed}";
            }
            catch
            {
                // ignore parse errors
            }

            return $"{msg}: {body}";
        }

        private static bool TryGetPropertyCI(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in obj.EnumerateObject())
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = p.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string? GetString(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyCI(obj, name, out var p)) continue;
                if (p.ValueKind == JsonValueKind.String) return p.GetString();
                return p.ToString();
            }
            return null;
        }

        private static string? GetRawText(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyCI(obj, name, out var p)) continue;
                return p.ToString();
            }
            return null;
        }

        private static long GetLong(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyCI(obj, name, out var p)) continue;
                if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n)) return n;
                if (p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), out var s)) return s;
            }
            return 0;
        }

        private static DateTime? GetDateTime(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetPropertyCI(obj, name, out var p)) continue;

                if (p.ValueKind == JsonValueKind.String && DateTime.TryParse(p.GetString(), out var dt))
                    return dt;

                try
                {
                    return p.GetDateTime();
                }
                catch
                {
                    // ignore and continue
                }
            }

            return null;
        }
    }

    public class VeMayBayItemDto
    {
        public long Id { get; set; }
        public long IdLichBay { get; set; }
        public DateTime? NgayDat { get; set; }
        public string DiemDi { get; set; } = "";
        public string DiemDen { get; set; } = "";
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string? TrangThaiRaw { get; set; }
    }

    public class VeMayBayDetailDto
    {
        public long Id { get; set; }
        public DateTime? NgayDat { get; set; }
        public string DiemDi { get; set; } = "";
        public string DiemDen { get; set; } = "";
        public string MaSanBayDi { get; set; } = "";
        public string MaSanBayDen { get; set; } = "";
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
    }
}
