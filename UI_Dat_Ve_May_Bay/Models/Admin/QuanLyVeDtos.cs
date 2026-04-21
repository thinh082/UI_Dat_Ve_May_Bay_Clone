using System;
using System.Collections.Generic;

namespace UI_Dat_Ve_May_Bay.Models.Admin
{
    public class QuanLyVeLocDatVeAdminModel
    {
        public long? MaDatVe { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public long? IdChuyenBay { get; set; }
        public long? IdLichBay { get; set; }
        public string? MaSanBayDi { get; set; }
        public string? MaSanBayDen { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? NgayDatFrom { get; set; }
        public DateTime? NgayDatTo { get; set; }
    }

    public class QuanLyVeCapNhatTrangThaiVeModel
    {
        public long IdDatVe { get; set; }
        public string TrangThaiMoi { get; set; } = string.Empty;
    }

    public class QuanLyVeDanhSachItemDto
    {
        public long Id { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? NgayDat { get; set; }
        public decimal? Gia { get; set; }
        public long? IdChuyenBay { get; set; }
        public string? MaSanBayDi { get; set; }
        public string? MaSanBayDen { get; set; }
        public long? LichBayId { get; set; }
        public int SoGhe { get; set; }

        public string GiaText => Gia.HasValue ? $"{Gia.Value:N0} VNĐ" : string.Empty;
        public string NgayDatText => NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;
        public string HanhTrinhText => $"{MaSanBayDi ?? "-"} -> {MaSanBayDen ?? "-"}";
    }

    public class QuanLyVeChiTietGheDto
    {
        public long Id { get; set; }
        public long? IdGheNgoi { get; set; }
        public string? SoGhe { get; set; }
        public int? IdLoaiVe { get; set; }

        public string LoaiVeText => IdLoaiVe?.ToString() ?? string.Empty;
    }

    public class QuanLyVeChiTietDto
    {
        public long Id { get; set; }
        public long? IdTaiKhoan { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public long? IdChuyenBay { get; set; }
        public string? SanBayDi { get; set; }
        public string? SanBayDen { get; set; }
        public long? LichBayId { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? NgayDat { get; set; }
        public DateTime? NgayHuy { get; set; }
        public decimal? Gia { get; set; }
        public List<QuanLyVeChiTietGheDto>? ChiTiet { get; set; }

        public string GiaText => Gia.HasValue ? $"{Gia.Value:N0} VNĐ" : string.Empty;
        public string NgayDatText => NgayDat?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;
        public string NgayHuyText => NgayHuy?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;
    }
}
