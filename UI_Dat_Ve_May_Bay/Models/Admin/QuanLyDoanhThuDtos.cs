namespace UI_Dat_Ve_May_Bay.Models.Admin
{
    public class QuanLyDoanhThuResultDto
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public decimal DoanhThu { get; set; }

        public string DoanhThuText => $"{DoanhThu:N0} VND";
    }
}
