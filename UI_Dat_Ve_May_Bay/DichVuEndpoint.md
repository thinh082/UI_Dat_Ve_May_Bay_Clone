### CONTROLLER
public class DichVuController : ControllerBase
    {
        private readonly IDichVuService _service;
        public DichVuController(IDichVuService dichVuService)
        {
            _service = dichVuService;
        }
        [HttpGet("LayDanhSachDichVu")]
        public async Task<IActionResult> LayDanhSachDichVu()
        {
            var idTaiKhoanClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idTaiKhoanClaim == null || !long.TryParse(idTaiKhoanClaim.Value, out long idTaiKhoan))
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = "Người dùng chưa đăng nhập hoặc token không hợp lệ"
                });
            }
            if (idTaiKhoan <= 0)
            {
                return Ok(new
                {
                    statusCode = 500,
                    message = "ID tài khoản không hợp lệ!"
                });
            }
            var result = await _service.LayDanhSachDichVu(idTaiKhoan);
            return Ok(result);
        }
        [HttpGet("ChiTietDichVu")]
        public async Task<IActionResult> ChiTietDichVu([FromQuery] long idDichVu, [FromQuery] int loaiDichVu)
        {
            if (idDichVu <= 0 || (loaiDichVu != 1 && loaiDichVu != 2))
            {
                return Ok(new
                {
                    statusCode = 500,
                    message = "Tham số không hợp lệ!"
                });
            }
            var result = await _service.ChiTietDichVu(idDichVu, loaiDichVu);
            return Ok(result);
        }
    }
    ### SERVICE
    public class DichVuService : IDichVuService
    {
        private readonly ThinhContext _context;
        public DichVuService(ThinhContext thinhContext)
        {
            _context = thinhContext;
        }
        public async Task<dynamic> LayDanhSachDichVu(long idTaiKhoan)
        {
            try
            {
                // === Vé máy bay ===
                var veMayBay = await _context.DatVes
                    .Where(r => r.IdTaiKhoan == idTaiKhoan)
                    .Select(r => new
                    {
                        LoaiDichVu = "VeMayBay",
                        Id = r.Id,
                        IdLichBay = r.IdLichBay,
                        NgayDat = r.NgayDat,
                        DiemDi = r.IdChuyenBayNavigation.MaSanBayDiNavigation.Ten,
                        DiemDen = r.IdChuyenBayNavigation.MaSanBayDiNavigation.Ten,
                        ThoiGianBatDau = r.LichBay.ThoiGianOsanBayDiUtc,                          
                        ThoiGianKetThuc = r.LichBay.ThoiGianOsanBayDenUtc,
                        TrangThai = r.TrangThai,
                    })
                    .OrderByDescending(r => r.NgayDat)
                    .ToListAsync();

                var hotel = await _context.DatPhongs.Where(r => r.IdTaiKhoan == idTaiKhoan).Select(r => new
                {
                    r.Id,
                    r.NgayDat,
                    r.IdPhongNavigation.Gia,
                    r.IdPhongNavigation.Hinh,
                    r.IdPhongNavigation.TenPhong,
                    r.IdPhongNavigation.SoGiuong,
                    r.IdPhongNavigation.MoTa
                }).OrderByDescending(r => r.NgayDat).ToListAsync();
                return new
                {
                    statusCode = 200,
                    message = "Lấy danh sách dịch vụ thành công",
                    data = new
                    {
                        VeMayBay = veMayBay,
                        Hotel = hotel
                    }
                };
            }catch(Exception ex)
            {
                return new
                {
                    statusCode = 500,
                    message = "Lấy danh sách dịch vụ thất bại: " + ex.Message
                };
            }
        }
        public async Task<dynamic> ChiTietDichVu(long idDichVu,int loaiDichVu)
        {
            if(loaiDichVu == 1)
            {
                var dichVu = await _context.DatVes.Where(r=>r.Id == idDichVu)
                     .Select(r => new
                     {
                         Id = r.Id,
                         NgayDat = r.NgayDat,
                         DiemDi = r.IdChuyenBayNavigation.MaSanBayDiNavigation.Ten,
                         DiemDen = r.IdChuyenBayNavigation.MaSanBayDenNavigation.Ten,
                         MaSanBayDi = r.IdChuyenBayNavigation.MaSanBayDi,
                         MaSanBayDen = r.IdChuyenBayNavigation.MaSanBayDen,
                         ThoiGianBatDau = r.LichBay.ThoiGianOsanBayDiUtc,
                         ThoiGianKetThuc = r.LichBay.ThoiGianOsanBayDenUtc
                     }).FirstOrDefaultAsync();
                return new
                {
                    statusCode = 200,
                    message = "Lấy chi tiết dịch vụ vé máy bay thành công",
                    data = dichVu
                };
            }
            else
            {
                return new
                {
                    statusCode = 200,
                    message = "Chức năng đang được phát triển"
                };
            }
        }

    }