using System;

namespace UI_Dat_Ve_May_Bay.Models.Notifications
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public long? IdTaiKhoan { get; set; }
        public string? TieuDe { get; set; }
        public string? NoiDung { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? HinhAnh { get; set; }
    }

    // wrapper cho ChiTietThongBao (BE trả về { statusCode, message, data })
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
