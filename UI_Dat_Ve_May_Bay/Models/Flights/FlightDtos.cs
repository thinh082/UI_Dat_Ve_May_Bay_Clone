using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UI_Dat_Ve_May_Bay.Models.Flights
{
    public class TimChuyenBayRequest
    {
        [JsonPropertyName("idLoaiVe")] public int IdLoaiVe { get; set; }
        [JsonPropertyName("idHangBay")] public int IdHangBay { get; set; }
        [JsonPropertyName("maSanBayDi")] public string MaSanBayDi { get; set; } = "";
        [JsonPropertyName("maSanBayDen")] public string MaSanBayDen { get; set; } = "";

        // ✅ BE parse dd-MM-yyyy
        [JsonPropertyName("ngayDi")] public string NgayDi { get; set; } = "";

        [JsonPropertyName("giaMin")] public decimal GiaMin { get; set; }
        [JsonPropertyName("giaMax")] public decimal GiaMax { get; set; }

        // BE nhận string (vd "1,2,3")
        [JsonPropertyName("idTienNghi")] public string IdTienNghi { get; set; } = "1";
    }

    // ✅ statusCode của BE lúc int(200) lúc bool(false)
    public class ApiResponse<T>
    {
        [JsonPropertyName("statusCode")] public JsonElement StatusCode { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("data")] public T? Data { get; set; }

        public bool IsSuccess
        {
            get
            {
                if (StatusCode.ValueKind == JsonValueKind.Number)
                    return StatusCode.GetInt32() == 200;

                if (StatusCode.ValueKind == JsonValueKind.True) return true;
                if (StatusCode.ValueKind == JsonValueKind.False) return false;

                return false;
            }
        }
    }

    public class FlightGroupDto
    {
        [JsonPropertyName("hangBay")] public HangBayDto? HangBay { get; set; }
        [JsonPropertyName("sanBayDi")] public SanBayDto? SanBayDi { get; set; }
        [JsonPropertyName("sanBayDen")] public SanBayDto? SanBayDen { get; set; }
        [JsonPropertyName("lichBay")] public List<LichBayDto> LichBay { get; set; } = new();
    }

    public class HangBayDto
    {
        [JsonPropertyName("idHangBay")] public long IdHangBay { get; set; }
        [JsonPropertyName("tenHangBay")] public string TenHangBay { get; set; } = "";
    }

    public class SanBayDto
    {
        [JsonPropertyName("maSanBay")] public string MaSanBay { get; set; } = "";
        [JsonPropertyName("tenSanBay")] public string TenSanBay { get; set; } = "";
    }

    public class LichBayDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }

        // ⚠️ BE đặt tên IdTuyenBay nhưng thực tế FK -> ChuyenBay.Id
        [JsonPropertyName("idTuyenBay")] public long IdTuyenBay { get; set; }

        [JsonPropertyName("thoiGianOsanBayDiUtc")] public DateTime ThoiGianOsanBayDiUtc { get; set; }
        [JsonPropertyName("thoiGianOsanBayDenUtc")] public DateTime ThoiGianOsanBayDenUtc { get; set; }

        [JsonPropertyName("thoiGianBay")] public int ThoiGianBay { get; set; }
        [JsonPropertyName("gia")] public decimal Gia { get; set; }
        [JsonPropertyName("soLuongGhe")] public int SoLuongGhe { get; set; }

        [JsonPropertyName("tenTienIch")] public List<string> TenTienIch { get; set; } = new();
    }

    // ================== SEAT (BE trả Dictionary<string, List<GheNgoiDto>>) ==================
    public class GheNgoiDto
    {
        [JsonPropertyName("idGheNgoi")] public long IdGheNgoi { get; set; }
        [JsonPropertyName("soGhe")] public string SoGhe { get; set; } = "";
        [JsonPropertyName("idLoaiVe")] public int IdLoaiVe { get; set; }
        [JsonPropertyName("giaGhe")] public decimal GiaGhe { get; set; }

        // 0 free, 2 held, 1/.. tuỳ BE
        [JsonPropertyName("idTrangThai")] public int IdTrangThai { get; set; }
    }
    // BE: Models/Model/SetGheNgoiModel.cs
    public class SetGheNgoiRequestDto
    {
        public long IdLichBay { get; set; }
        public List<long> IdGheNgoi { get; set; } = new();
    }

    // BE: Models/Model/DatVeRequest.cs
    public class DatVeRequestDto
    {
        public List<long> IdGheNgois { get; set; } = new();
        public long IdChuyenBay { get; set; }
        public long IdTuyenBay { get; set; }
        public long IdLichBay { get; set; }
        public long? IdChiTietPhieuGiamGia { get; set; }
        public decimal Gia { get; set; }
    }

    // BE trả về ghế
    public class SeatGroupsDto
    {
        public List<GheNgoiDto> LoaiVe1 { get; set; } = new();
        public List<GheNgoiDto> LoaiVe3 { get; set; } = new();
        public List<GheNgoiDto> LoaiVe4 { get; set; } = new();
    }

    // Wrapper cho các API của ChuyenBayController: statusCode là BOOL
    public class ApiBoolResponse<T>
    {
        public bool StatusCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}