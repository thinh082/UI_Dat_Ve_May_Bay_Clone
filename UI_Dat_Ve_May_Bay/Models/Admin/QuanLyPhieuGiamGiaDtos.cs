using System;

namespace UI_Dat_Ve_May_Bay.Models.Admin
{
    public class QuanLyPhieuGiamGiaItemDto
    {
        public long Id { get; set; }
        public string MaGiamGia { get; set; } = string.Empty;
        public decimal GiaTriGiam { get; set; }
        public DateOnly NgayKetThuc { get; set; }
        public string? NoiDung { get; set; }
        public bool? Active { get; set; }
        public string? LoaiGiamGia { get; set; }

        public string NgayKetThucText => NgayKetThuc.ToString("dd/MM/yyyy");
        public string GiaTriGiamText => $"{GiaTriGiam:N0}";
        public string TrangThaiText => (Active ?? false) ? "Đang bật" : "Đang tắt";
    }

    public class QuanLyPhieuGiamGiaUpsertModel
    {
        public long Id { get; set; }
        public string MaGiamGia { get; set; } = string.Empty;
        public decimal GiaTriGiam { get; set; }
        public DateOnly NgayKetThuc { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int IdLoaiGiamGia { get; set; }
    }
}
