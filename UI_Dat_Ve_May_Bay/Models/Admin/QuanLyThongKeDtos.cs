using System;

namespace UI_Dat_Ve_May_Bay.Models.Admin
{
    public class QuanLyThongKeDashboardDto
    {
        public decimal DoanhThuHomNay { get; set; }
        public int TongVeDaBan { get; set; }
        public int NguoiDungHoatDong { get; set; }
        public double TiLeHuy { get; set; }

        public string DoanhThuHomNayText => $"{DoanhThuHomNay:N0} VND";
        public string TiLeHuyText => $"{TiLeHuy:0.##}%";
    }

    public class QuanLyThongKeDoanhThuNgayDto
    {
        public DateTime Ngay { get; set; }
        public decimal DoanhThu { get; set; }

        public string NgayText => Ngay.ToString("dd/MM");
        public string DoanhThuText => $"{DoanhThu:N0} VND";
    }

    public class QuanLyThongKeVeNgayDto
    {
        public DateTime Ngay { get; set; }
        public int TongVe { get; set; }
        public int VeDaDat { get; set; }
        public int VeDaCheckin { get; set; }
        public int VeDaHuy { get; set; }

        public string NgayText => Ngay.ToString("dd/MM");
    }
}
