using System;
using System.Collections.Generic;

namespace UI_Dat_Ve_May_Bay.Models.Admin
{
    public class QuanLyLichSuThanhToanFilterModel
    {
        public DateTime? NgayThanhToanFrom { get; set; }
        public DateTime? NgayThanhToanTo { get; set; }
        public decimal? SoTienMin { get; set; }
        public decimal? SoTienMax { get; set; }
    }

    public class QuanLyLichSuThanhToanItemDto
    {
        public long Id { get; set; }
        public string MaThanhToan { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string? TenKhachHang { get; set; }
        public decimal SoTien { get; set; }
        public string? PhuongThucThanhToan { get; set; }
        public string? TrangThai { get; set; }
        public DateTime NgayThanhToan { get; set; }
        public string? LoaiDichVu { get; set; }
        public bool IsVnPay { get; set; }
        public bool IsPayPal { get; set; }

        public string SoTienText => $"{SoTien:N0} VND";
        public string NgayThanhToanText => NgayThanhToan.ToString("dd/MM/yyyy HH:mm");
        public string KhachHangText => !string.IsNullOrWhiteSpace(TenKhachHang) ? TenKhachHang! : (Email ?? "--");
    }

    public class QuanLyLichSuThanhToanPaginationDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class QuanLyLichSuThanhToanPagedResult
    {
        public List<QuanLyLichSuThanhToanItemDto> Items { get; set; } = new();
        public QuanLyLichSuThanhToanPaginationDto Pagination { get; set; } = new();
        public string? Message { get; set; }
    }

    public class QuanLyLichSuThanhToanDetailDto
    {
        public long Id { get; set; }
        public string MaThanhToan { get; set; } = string.Empty;
        public QuanLyLichSuThanhToanTaiKhoanDto? TaiKhoan { get; set; }
        public decimal SoTien { get; set; }
        public QuanLyLichSuThanhToanPhuongThucDto? PhuongThucThanhToan { get; set; }
        public QuanLyLichSuThanhToanTrangThaiDto? TrangThai { get; set; }
        public DateTime NgayThanhToan { get; set; }
        public string? LoaiDichVu { get; set; }
        public bool? VnPayId { get; set; }
        public bool? PayPalId { get; set; }

        public string SoTienText => $"{SoTien:N0} VND";
        public string NgayThanhToanText => NgayThanhToan.ToString("dd/MM/yyyy HH:mm");
    }

    public class QuanLyLichSuThanhToanTaiKhoanDto
    {
        public long Id { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string? TenKhachHang { get; set; }
    }

    public class QuanLyLichSuThanhToanPhuongThucDto
    {
        public int Id { get; set; }
        public string? TenPhuongThuc { get; set; }
    }

    public class QuanLyLichSuThanhToanTrangThaiDto
    {
        public int Id { get; set; }
        public string? TenTrangThai { get; set; }
    }

    public class QuanLyLichSuThanhToanThongKeItemDto
    {
        public int IdPhuongThucThanhToan { get; set; }
        public int TrangThaiId { get; set; }
        public string? TenPhuongThuc { get; set; }
        public string? TenTrangThai { get; set; }
        public int SoLuong { get; set; }
        public decimal TongTien { get; set; }
        public decimal TrungBinh { get; set; }

        public string Label => !string.IsNullOrWhiteSpace(TenPhuongThuc) ? TenPhuongThuc! : (TenTrangThai ?? "--");
        public string TongTienText => $"{TongTien:N0} VND";
        public string TrungBinhText => $"{TrungBinh:N0} VND";
    }

    public class QuanLyLichSuThanhToanTongQuanDto
    {
        public int TongSoGiaoDich { get; set; }
        public decimal TongTien { get; set; }
        public decimal TrungBinhGiaTri { get; set; }
        public int GiaoDichThanhCong { get; set; }
        public int GiaoDichThatBai { get; set; }

        public string TongTienText => $"{TongTien:N0} VND";
        public string TrungBinhGiaTriText => $"{TrungBinhGiaTri:N0} VND";
    }
}
