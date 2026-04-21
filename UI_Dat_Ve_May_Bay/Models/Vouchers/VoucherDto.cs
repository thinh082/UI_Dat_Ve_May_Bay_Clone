using System;

namespace UI_Dat_Ve_May_Bay.Models.Vouchers
{
    public class VoucherDto
    {
        public long Id { get; set; }
        public string? MaGiamGia { get; set; }
        public decimal GiaTriGiam { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string? NoiDung { get; set; }
        public bool Active { get; set; }
        public string? LoaiGiamGia { get; set; } // "Phần Trăm" | "Tiền"

        // tiện bind UI
        // tiện bind UI
        public string DisplayCode
        {
            get => MaGiamGia ?? "";
            set { /* ignore */ }
        }

        public string DisplayContent
        {
            get => NoiDung ?? "";
            set { /* ignore */ }
        }

        public string DisplayExpiry
        {
            get => NgayKetThuc.ToString("dd/MM/yyyy");
            set { /* ignore */ }
        }

        public string DisplayValue
        {
            get
            {
                if (LoaiGiamGia == "Phần Trăm")
                {
                    if (GiaTriGiam > 100) return $"{GiaTriGiam:n0} đ";
                    return $"{GiaTriGiam:0.##}%";
                }
                return $"{GiaTriGiam:n0} đ";
            }
            set { /* ignore */ }
        }

    }
}
