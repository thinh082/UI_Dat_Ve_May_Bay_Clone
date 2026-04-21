using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UI_Dat_Ve_May_Bay.Models.Admin
{
    public class QuanLyNguoiDungFilterModel
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("soDienThoai")]
        public string? SoDienThoai { get; set; }

        [JsonPropertyName("tenKhachHang")]
        public string? TenKhachHang { get; set; }

        [JsonPropertyName("loaiTaiKhoanId")]
        public int? LoaiTaiKhoanId { get; set; }

        [JsonPropertyName("idQuocTich")]
        public int? IdQuocTich { get; set; }
    }

    public class QuanLyNguoiDungItemDto
    {
        public long Id { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public int? LoaiTaiKhoanId { get; set; }
        public string? TenLoaiTaiKhoan { get; set; }
        public string? TenKhachHang { get; set; }
        public int? IdQuocTich { get; set; }
        public string? TenQuocTich { get; set; }
        public string? HinhAnh { get; set; }
        public int SoDatVe { get; set; }
        public int SoDatPhong { get; set; }
        public bool CoCCCD { get; set; }
        public int SoPassport { get; set; }

        public string DisplayName => !string.IsNullOrWhiteSpace(TenKhachHang) ? TenKhachHang! : Email ?? "--";
        public string ContactText => !string.IsNullOrWhiteSpace(SoDienThoai) ? SoDienThoai! : "--";
        public string RoleText => !string.IsNullOrWhiteSpace(TenLoaiTaiKhoan) ? TenLoaiTaiKhoan! : "Nguoi dung";
        public string QuocTichText => !string.IsNullOrWhiteSpace(TenQuocTich) ? TenQuocTich! : "--";
        public string BookingSummaryText => $"{SoDatVe} ve / {SoDatPhong} phong";
        public string IdentitySummaryText => CoCCCD ? $"CCCD, {SoPassport} passport" : $"{SoPassport} passport";
    }

    public class QuanLyNguoiDungDetailDto
    {
        public QuanLyNguoiDungTaiKhoanDto? TaiKhoan { get; set; }
        public QuanLyNguoiDungKhachHangDto? KhachHang { get; set; }
        public QuanLyNguoiDungCccdDto? KhachHangCccd { get; set; }
        public List<QuanLyNguoiDungPassportDto> KhachHangPassports { get; set; } = new();
        public List<QuanLyNguoiDungDatVeDto> LichSuDatVe { get; set; } = new();
        public List<QuanLyNguoiDungThanhToanDto> LichSuThanhToan { get; set; } = new();
    }

    public class QuanLyNguoiDungTaiKhoanDto
    {
        public long Id { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public int? LoaiTaiKhoanId { get; set; }
        public QuanLyNguoiDungLoaiTaiKhoanDto? LoaiTaiKhoan { get; set; }
        public string? HinhAnh { get; set; }
        public bool? IsEmail { get; set; }
        public DateTime? NgayTao { get; set; }
    }

    public class QuanLyNguoiDungLoaiTaiKhoanDto
    {
        public int Id { get; set; }
        public string? TenLoai { get; set; }
    }

    public class QuanLyNguoiDungKhachHangDto
    {
        public long Id { get; set; }
        public string? TenKh { get; set; }
        public string? DiaChi { get; set; }
        public int? GioiTinh { get; set; }
        public int? IdPhuong { get; set; }
        public QuanLyNguoiDungPhuongDto? Phuong { get; set; }
        public int? IdQuan { get; set; }
        public QuanLyNguoiDungQuanDto? Quan { get; set; }
        public int? IdTinh { get; set; }
        public QuanLyNguoiDungTinhDto? Tinh { get; set; }
        public int? IdQuocTich { get; set; }
        public QuanLyNguoiDungQuocTichDto? QuocTich { get; set; }
    }

    public class QuanLyNguoiDungPhuongDto
    {
        public int IdPhuong { get; set; }
        public string? TenPhuong { get; set; }
    }

    public class QuanLyNguoiDungQuanDto
    {
        public int IdQuan { get; set; }
        public string? TenQuan { get; set; }
    }

    public class QuanLyNguoiDungTinhDto
    {
        public int IdTinh { get; set; }
        public string? TenTinh { get; set; }
    }

    public class QuanLyNguoiDungQuocTichDto
    {
        public int Id { get; set; }
        public string? QuocTich1 { get; set; }
    }

    public class QuanLyNguoiDungCccdDto
    {
        public long Id { get; set; }
        public string? SoCccd { get; set; }
        public string? TenTrenCccd { get; set; }
        public DateTime? NgayCap { get; set; }
        public string? NoiCap { get; set; }
        public string? NoiThuongTru { get; set; }
        public string? QueQuan { get; set; }
    }

    public class QuanLyNguoiDungPassportDto
    {
        public long Id { get; set; }
        public string? SoPassport { get; set; }
        public string? TenTrenPassport { get; set; }
        public DateTime? NgayCap { get; set; }
        public DateTime? NgayHetHan { get; set; }
        public string? NoiCap { get; set; }
        public string? QuocTich { get; set; }
        public string? LoaiPassport { get; set; }
        public string? GhiChu { get; set; }
    }

    public class QuanLyNguoiDungDatVeDto
    {
        public long Id { get; set; }
        public DateTime? NgayDat { get; set; }
        public string? TrangThai { get; set; }
        public decimal? Gia { get; set; }
        public string? TenChuyenBay { get; set; }

        public string NgayDatText => NgayDat.HasValue ? NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "--";
        public string GiaText => Gia.HasValue ? $"{Gia.Value:N0} VND" : "--";
    }

    public class QuanLyNguoiDungThanhToanDto
    {
        public long Id { get; set; }
        public string? MaThanhToan { get; set; }
        public DateTime? NgayThanhToan { get; set; }
        public decimal? SoTien { get; set; }
        public string? LoaiDichVu { get; set; }
        public string? TenPhuongThuc { get; set; }

        public string NgayThanhToanText => NgayThanhToan.HasValue ? NgayThanhToan.Value.ToString("dd/MM/yyyy HH:mm") : "--";
        public string SoTienText => SoTien.HasValue ? $"{SoTien.Value:N0} VND" : "--";
    }

    public class QuanLyNguoiDungUpdateModel
    {
        [JsonPropertyName("idTaiKhoan")]
        public long IdTaiKhoan { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("soDienThoai")]
        public string? SoDienThoai { get; set; }

        [JsonPropertyName("loaiTaiKhoanId")]
        public int? LoaiTaiKhoanId { get; set; }

        [JsonPropertyName("tenKhachHang")]
        public string? TenKhachHang { get; set; }

        [JsonPropertyName("diaChi")]
        public string? DiaChi { get; set; }

        [JsonPropertyName("gioiTinh")]
        public int? GioiTinh { get; set; }

        [JsonPropertyName("idPhuong")]
        public int? IdPhuong { get; set; }

        [JsonPropertyName("idQuan")]
        public int? IdQuan { get; set; }

        [JsonPropertyName("idTinh")]
        public int? IdTinh { get; set; }

        [JsonPropertyName("idQuocTich")]
        public int? IdQuocTich { get; set; }
    }
}
