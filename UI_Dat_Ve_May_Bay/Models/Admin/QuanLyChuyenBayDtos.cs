using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UI_Dat_Ve_May_Bay.Models.Admin
{
    public class QuanLyChuyenBayFilterModel
    {
        public string? MaSanBayDi { get; set; }
        public string? MaSanBayDen { get; set; }
        public decimal? GiaMin { get; set; }
        public decimal? GiaMax { get; set; }
        public string? TenHangBay { get; set; }
    }

    public class QuanLyChuyenBayItemDto
    {
        public long Id { get; set; }
        public int IdHangBay { get; set; }
        public string? TenHangBay { get; set; }
        public string? MaSanBayDi { get; set; }
        public string? TenSanBayDi { get; set; }
        public string? MaSanBayDen { get; set; }
        public string? TenSanBayDen { get; set; }
        public int SoLichBay { get; set; }
        public int SoGheNgoi { get; set; }
        public decimal? GiaMin { get; set; }
        public decimal? GiaMax { get; set; }
        public List<QuanLyChuyenBayLichBayDto> LichBayHomNay { get; set; } = new();

        public string HanhTrinhText => $"{TenSanBayDi ?? MaSanBayDi ?? "--"} -> {TenSanBayDen ?? MaSanBayDen ?? "--"}";
        public string GiaText
        {
            get
            {
                if (GiaMin.HasValue && GiaMax.HasValue)
                    return GiaMin.Value == GiaMax.Value ? $"{GiaMin.Value:N0} VND" : $"{GiaMin.Value:N0} - {GiaMax.Value:N0} VND";
                if (GiaMin.HasValue) return $"{GiaMin.Value:N0} VND";
                if (GiaMax.HasValue) return $"{GiaMax.Value:N0} VND";
                return "--";
            }
        }
    }

    public class QuanLyChuyenBayDetailDto
    {
        public long Id { get; set; }
        public int IdHangBay { get; set; }
        public QuanLyChuyenBayHangBayDto? HangBay { get; set; }
        public string? MaSanBayDi { get; set; }
        public QuanLyChuyenBaySanBayDto? SanBayDi { get; set; }
        public string? MaSanBayDen { get; set; }
        public QuanLyChuyenBaySanBayDto? SanBayDen { get; set; }
        public int SoLichBay { get; set; }
        public int SoGheNgoi { get; set; }
        public int SoDatVe { get; set; }
        public List<QuanLyChuyenBayLichBayDto> LichBayHomNay { get; set; } = new();
        public List<QuanLyChuyenBayTienNghiDto> TienNghi { get; set; } = new();

        public string HanhTrinhText => $"{SanBayDi?.Ten ?? MaSanBayDi ?? "--"} -> {SanBayDen?.Ten ?? MaSanBayDen ?? "--"}";
    }

    public class QuanLyChuyenBayHangBayDto
    {
        public int Id { get; set; }
        public string? TenHang { get; set; }
    }

    public class QuanLyChuyenBaySanBayDto
    {
        public string? MaIata { get; set; }
        public string? Ten { get; set; }
        public string? ThanhPho { get; set; }
        public string? QuocGia { get; set; }
        public string? TimeZoneId { get; set; }
        public string? IanaTimeZoneId { get; set; }

        public string ViTriText => $"{ThanhPho ?? "--"}, {QuocGia ?? "--"}";
    }

    public class QuanLyChuyenBayLichBayDto
    {
        public long Id { get; set; }
        public long IdTuyenBay { get; set; }
        public DateTime ThoiGianOsanBayDiUtc { get; set; }
        public DateTime ThoiGianOsanBayDenUtc { get; set; }
        public int? ThoiGianBay { get; set; }
        public decimal? Gia { get; set; }

        public string ThoiGianDiText => ThoiGianOsanBayDiUtc.ToString("dd/MM/yyyy HH:mm");
        public string ThoiGianDenText => ThoiGianOsanBayDenUtc.ToString("dd/MM/yyyy HH:mm");
        public string ThoiGianBayText => ThoiGianBay.HasValue ? $"{ThoiGianBay.Value} phút" : "--";
        public string GiaText => Gia.HasValue ? $"{Gia.Value:N0} VND" : "--";
    }

    public class QuanLyChuyenBayTienNghiDto
    {
        public long Id { get; set; }
        public int IdTienNghi { get; set; }
        public string? TenTienNghi { get; set; }
        public string? MaTienIch { get; set; }
    }

    public class QuanLyChuyenBaySaveModel
    {
        [JsonPropertyName("idChuyenBay")]
        public long IdChuyenBay { get; set; }

        [JsonPropertyName("idHangBay")]
        public int IdHangBay { get; set; }

        [JsonPropertyName("maSanBayDi")]
        public string MaSanBayDi { get; set; } = string.Empty;

        [JsonPropertyName("maSanBayDen")]
        public string MaSanBayDen { get; set; } = string.Empty;
    }
}
