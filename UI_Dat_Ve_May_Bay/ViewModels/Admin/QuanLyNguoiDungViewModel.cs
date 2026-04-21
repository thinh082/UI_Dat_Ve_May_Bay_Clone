using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Api.Admin;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Models.Admin;

namespace UI_Dat_Ve_May_Bay.ViewModels.Admin
{
    public class QuanLyNguoiDungViewModel : ObservableObject, IAdminRefreshable
    {
        private readonly QuanLyNguoiDungApi _api;

        public ObservableCollection<QuanLyNguoiDungItemDto> DanhSachNguoiDung { get; } = new();
        public ObservableCollection<QuanLyNguoiDungDatVeDto> LichSuDatVe { get; } = new();
        public ObservableCollection<QuanLyNguoiDungThanhToanDto> LichSuThanhToan { get; } = new();
        public ObservableCollection<QuanLyNguoiDungPassportDto> DanhSachPassport { get; } = new();

        public QuanLyNguoiDungViewModel(QuanLyNguoiDungApi api)
        {
            _api = api;

            LoadDanhSachCommand = new AsyncRelayCommand(LoadDanhSachAsync, () => !IsBusy);
            TaiChiTietCommand = new AsyncRelayCommand(TaiChiTietAsync, () => !IsBusy && SelectedNguoiDung != null);
            LuuNguoiDungCommand = new AsyncRelayCommand(LuuNguoiDungAsync, () => !IsBusy && SelectedNguoiDung != null);
            XoaNguoiDungCommand = new AsyncRelayCommand(XoaNguoiDungAsync, () => !IsBusy && SelectedNguoiDung != null);
            OpenEditorCommand = new RelayCommand(OpenEditor);
            CloseEditorCommand = new RelayCommand(CloseEditor);
            ResetBoLocCommand = new RelayCommand(ResetBoLoc);
            ResetFormCommand = new RelayCommand(ResetForm);

            _ = LoadDanhSachAsync();
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                    RaiseCommandStates();
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                    AdminUiService.Publish(value);
            }
        }

        public int TongSoNguoiDung => DanhSachNguoiDung.Count;
        public int SoNguoiDungCoQuocTich => DanhSachNguoiDung.Count(x => x.IdQuocTich.HasValue && x.IdQuocTich.Value > 0);
        public int SoNguoiDungCoHoSo => DanhSachNguoiDung.Count(x => x.CoCCCD || x.SoPassport > 0);
        public string SummaryBadgeText => $"{TongSoNguoiDung} nguoi dung";

        private bool _isEditorOpen;
        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            set => SetProperty(ref _isEditorOpen, value);
        }

        private QuanLyNguoiDungItemDto? _selectedNguoiDung;
        public QuanLyNguoiDungItemDto? SelectedNguoiDung
        {
            get => _selectedNguoiDung;
            set
            {
                if (SetProperty(ref _selectedNguoiDung, value))
                {
                    RaiseCommandStates();
                    OnPropertyChanged(nameof(SelectedNguoiDungLabel));
                    OnPropertyChanged(nameof(SelectedNguoiDungStatusText));
                    if (value != null)
                    {
                        NapFormTuDanhSach(value);
                        _ = TaiChiTietAsync();
                        IsEditorOpen = true;
                    }
                }
            }
        }

        private QuanLyNguoiDungDetailDto? _chiTietNguoiDung;
        public QuanLyNguoiDungDetailDto? ChiTietNguoiDung
        {
            get => _chiTietNguoiDung;
            set
            {
                if (SetProperty(ref _chiTietNguoiDung, value))
                {
                    OnPropertyChanged(nameof(SelectedNguoiDungDisplayName));
                    OnPropertyChanged(nameof(SelectedNguoiDungEmail));
                    OnPropertyChanged(nameof(SelectedNguoiDungPhone));
                    OnPropertyChanged(nameof(SelectedNguoiDungLoaiTaiKhoan));
                    OnPropertyChanged(nameof(SelectedNguoiDungNgayTao));
                    OnPropertyChanged(nameof(SelectedNguoiDungDiaChi));
                    OnPropertyChanged(nameof(SelectedNguoiDungQuocTich));
                    OnPropertyChanged(nameof(SelectedNguoiDungCccd));
                    OnPropertyChanged(nameof(SelectedNguoiDungBookingCount));
                    OnPropertyChanged(nameof(SelectedNguoiDungPaymentCount));
                    OnPropertyChanged(nameof(SelectedNguoiDungPassportCount));
                }
            }
        }

        public string SelectedNguoiDungLabel => SelectedNguoiDung != null ? $"#{SelectedNguoiDung.Id}" : "#--";
        public string SelectedNguoiDungStatusText => SelectedNguoiDung?.RoleText ?? "Chua chon";
        public string SelectedNguoiDungDisplayName => ChiTietNguoiDung?.KhachHang?.TenKh ?? SelectedNguoiDung?.DisplayName ?? "--";
        public string SelectedNguoiDungEmail => ChiTietNguoiDung?.TaiKhoan?.Email ?? SelectedNguoiDung?.Email ?? "--";
        public string SelectedNguoiDungPhone => ChiTietNguoiDung?.TaiKhoan?.SoDienThoai ?? SelectedNguoiDung?.SoDienThoai ?? "--";
        public string SelectedNguoiDungLoaiTaiKhoan => ChiTietNguoiDung?.TaiKhoan?.LoaiTaiKhoan?.TenLoai ?? SelectedNguoiDung?.RoleText ?? "--";
        public string SelectedNguoiDungNgayTao => ChiTietNguoiDung?.TaiKhoan?.NgayTao?.ToString("dd/MM/yyyy HH:mm") ?? "--";
        public string SelectedNguoiDungDiaChi
        {
            get
            {
                var kh = ChiTietNguoiDung?.KhachHang;
                if (kh == null) return "--";

                var values = new[]
                {
                    kh.DiaChi,
                    kh.Phuong?.TenPhuong,
                    kh.Quan?.TenQuan,
                    kh.Tinh?.TenTinh
                }.Where(x => !string.IsNullOrWhiteSpace(x));

                var text = string.Join(", ", values);
                return string.IsNullOrWhiteSpace(text) ? "--" : text;
            }
        }

        public string SelectedNguoiDungQuocTich => ChiTietNguoiDung?.KhachHang?.QuocTich?.QuocTich1 ?? SelectedNguoiDung?.TenQuocTich ?? "--";
        public string SelectedNguoiDungCccd => ChiTietNguoiDung?.KhachHangCccd?.SoCccd ?? "--";
        public string SelectedNguoiDungBookingCount => $"{LichSuDatVe.Count} giao dich ve";
        public string SelectedNguoiDungPaymentCount => $"{LichSuThanhToan.Count} giao dich thanh toan";
        public string SelectedNguoiDungPassportCount => $"{DanhSachPassport.Count} passport";

        private string _filterEmail = string.Empty;
        public string FilterEmail
        {
            get => _filterEmail;
            set => SetProperty(ref _filterEmail, value);
        }

        private string _filterSoDienThoai = string.Empty;
        public string FilterSoDienThoai
        {
            get => _filterSoDienThoai;
            set => SetProperty(ref _filterSoDienThoai, value);
        }

        private string _filterTenKhachHang = string.Empty;
        public string FilterTenKhachHang
        {
            get => _filterTenKhachHang;
            set => SetProperty(ref _filterTenKhachHang, value);
        }

        private string _filterLoaiTaiKhoanId = string.Empty;
        public string FilterLoaiTaiKhoanId
        {
            get => _filterLoaiTaiKhoanId;
            set => SetProperty(ref _filterLoaiTaiKhoanId, value);
        }

        private string _filterIdQuocTich = string.Empty;
        public string FilterIdQuocTich
        {
            get => _filterIdQuocTich;
            set => SetProperty(ref _filterIdQuocTich, value);
        }

        private string _formEmail = string.Empty;
        public string FormEmail
        {
            get => _formEmail;
            set => SetProperty(ref _formEmail, value);
        }

        private string _formSoDienThoai = string.Empty;
        public string FormSoDienThoai
        {
            get => _formSoDienThoai;
            set => SetProperty(ref _formSoDienThoai, value);
        }

        private string _formLoaiTaiKhoanId = string.Empty;
        public string FormLoaiTaiKhoanId
        {
            get => _formLoaiTaiKhoanId;
            set => SetProperty(ref _formLoaiTaiKhoanId, value);
        }

        private string _formTenKhachHang = string.Empty;
        public string FormTenKhachHang
        {
            get => _formTenKhachHang;
            set => SetProperty(ref _formTenKhachHang, value);
        }

        private string _formDiaChi = string.Empty;
        public string FormDiaChi
        {
            get => _formDiaChi;
            set => SetProperty(ref _formDiaChi, value);
        }

        private string _formGioiTinh = string.Empty;
        public string FormGioiTinh
        {
            get => _formGioiTinh;
            set => SetProperty(ref _formGioiTinh, value);
        }

        private string _formIdPhuong = string.Empty;
        public string FormIdPhuong
        {
            get => _formIdPhuong;
            set => SetProperty(ref _formIdPhuong, value);
        }

        private string _formIdQuan = string.Empty;
        public string FormIdQuan
        {
            get => _formIdQuan;
            set => SetProperty(ref _formIdQuan, value);
        }

        private string _formIdTinh = string.Empty;
        public string FormIdTinh
        {
            get => _formIdTinh;
            set => SetProperty(ref _formIdTinh, value);
        }

        private string _formIdQuocTich = string.Empty;
        public string FormIdQuocTich
        {
            get => _formIdQuocTich;
            set => SetProperty(ref _formIdQuocTich, value);
        }

        public AsyncRelayCommand LoadDanhSachCommand { get; }
        public AsyncRelayCommand TaiChiTietCommand { get; }
        public AsyncRelayCommand LuuNguoiDungCommand { get; }
        public AsyncRelayCommand XoaNguoiDungCommand { get; }
        public RelayCommand OpenEditorCommand { get; }
        public RelayCommand CloseEditorCommand { get; }
        public RelayCommand ResetBoLocCommand { get; }
        public RelayCommand ResetFormCommand { get; }

        private async Task LoadDanhSachAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var data = await _api.GetDanhSachNguoiDungAsync(new QuanLyNguoiDungFilterModel
                {
                    Email = EmptyToNull(FilterEmail),
                    SoDienThoai = EmptyToNull(FilterSoDienThoai),
                    TenKhachHang = EmptyToNull(FilterTenKhachHang),
                    LoaiTaiKhoanId = ParseNullableInt(FilterLoaiTaiKhoanId),
                    IdQuocTich = ParseNullableInt(FilterIdQuocTich)
                });

                DanhSachNguoiDung.Clear();
                foreach (var item in data.OrderBy(x => x.DisplayName))
                    DanhSachNguoiDung.Add(item);

                OnPropertyChanged(nameof(TongSoNguoiDung));
                OnPropertyChanged(nameof(SoNguoiDungCoQuocTich));
                OnPropertyChanged(nameof(SoNguoiDungCoHoSo));
                OnPropertyChanged(nameof(SummaryBadgeText));

                if (DanhSachNguoiDung.Count == 0)
                {
                    SelectedNguoiDung = null;
                    ChiTietNguoiDung = null;
                    LichSuDatVe.Clear();
                    LichSuThanhToan.Clear();
                    DanhSachPassport.Clear();
                    StatusMessage = "Khong tim thay nguoi dung phu hop.";
                }
                else
                {
                    SelectedNguoiDung ??= DanhSachNguoiDung.FirstOrDefault();
                    StatusMessage = $"Da tai {DanhSachNguoiDung.Count} nguoi dung.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Loi tai danh sach nguoi dung: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task TaiChiTietAsync()
        {
            if (SelectedNguoiDung == null) return;

            try
            {
                ChiTietNguoiDung = await _api.GetChiTietNguoiDungAsync(SelectedNguoiDung.Id);
                LichSuDatVe.Clear();
                foreach (var item in ChiTietNguoiDung?.LichSuDatVe ?? Enumerable.Empty<QuanLyNguoiDungDatVeDto>())
                    LichSuDatVe.Add(item);

                LichSuThanhToan.Clear();
                foreach (var item in ChiTietNguoiDung?.LichSuThanhToan ?? Enumerable.Empty<QuanLyNguoiDungThanhToanDto>())
                    LichSuThanhToan.Add(item);

                DanhSachPassport.Clear();
                foreach (var item in ChiTietNguoiDung?.KhachHangPassports ?? Enumerable.Empty<QuanLyNguoiDungPassportDto>())
                    DanhSachPassport.Add(item);

                NapFormTuChiTiet(ChiTietNguoiDung);
            }
            catch (Exception ex)
            {
                StatusMessage = "Loi tai chi tiet nguoi dung: " + ex.Message;
            }
        }

        private async Task LuuNguoiDungAsync()
        {
            if (SelectedNguoiDung == null) return;

            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var payload = new QuanLyNguoiDungUpdateModel
                {
                    IdTaiKhoan = SelectedNguoiDung.Id,
                    Email = EmptyToNull(FormEmail),
                    SoDienThoai = EmptyToNull(FormSoDienThoai),
                    LoaiTaiKhoanId = ParseNullableInt(FormLoaiTaiKhoanId),
                    TenKhachHang = EmptyToNull(FormTenKhachHang),
                    DiaChi = EmptyToNull(FormDiaChi),
                    GioiTinh = ParseNullableInt(FormGioiTinh),
                    IdPhuong = ParseNullableInt(FormIdPhuong),
                    IdQuan = ParseNullableInt(FormIdQuan),
                    IdTinh = ParseNullableInt(FormIdTinh),
                    IdQuocTich = ParseNullableInt(FormIdQuocTich)
                };

                var result = await _api.CapNhatNguoiDungAsync(payload);
                StatusMessage = result.Message ?? "Da cap nhat nguoi dung thanh cong.";
                await LoadDanhSachAsync();
                SelectedNguoiDung = DanhSachNguoiDung.FirstOrDefault(x => x.Id == payload.IdTaiKhoan);
                IsEditorOpen = false;
            }
            catch (Exception ex)
            {
                StatusMessage = "Loi cap nhat nguoi dung: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task XoaNguoiDungAsync()
        {
            if (SelectedNguoiDung == null) return;
            if (!AdminUiService.ConfirmDelete("Xoa nguoi dung nay?")) return;

            var idTaiKhoan = SelectedNguoiDung.Id;
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = await _api.XoaNguoiDungAsync(idTaiKhoan);
                StatusMessage = result.Message ?? "Da xoa nguoi dung.";
                ResetForm();
                await LoadDanhSachAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = "Loi xoa nguoi dung: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetBoLoc()
        {
            FilterEmail = string.Empty;
            FilterSoDienThoai = string.Empty;
            FilterTenKhachHang = string.Empty;
            FilterLoaiTaiKhoanId = string.Empty;
            FilterIdQuocTich = string.Empty;
            StatusMessage = string.Empty;
        }

        private void ResetForm()
        {
            FormEmail = string.Empty;
            FormSoDienThoai = string.Empty;
            FormLoaiTaiKhoanId = string.Empty;
            FormTenKhachHang = string.Empty;
            FormDiaChi = string.Empty;
            FormGioiTinh = string.Empty;
            FormIdPhuong = string.Empty;
            FormIdQuan = string.Empty;
            FormIdTinh = string.Empty;
            FormIdQuocTich = string.Empty;
            SelectedNguoiDung = null;
            ChiTietNguoiDung = null;
            LichSuDatVe.Clear();
            LichSuThanhToan.Clear();
            DanhSachPassport.Clear();
            IsEditorOpen = false;
        }

        private void NapFormTuDanhSach(QuanLyNguoiDungItemDto item)
        {
            FormEmail = item.Email ?? string.Empty;
            FormSoDienThoai = item.SoDienThoai ?? string.Empty;
            FormLoaiTaiKhoanId = item.LoaiTaiKhoanId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FormTenKhachHang = item.TenKhachHang ?? string.Empty;
            FormIdQuocTich = item.IdQuocTich?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private void NapFormTuChiTiet(QuanLyNguoiDungDetailDto? detail)
        {
            if (detail == null) return;

            FormEmail = detail.TaiKhoan?.Email ?? FormEmail;
            FormSoDienThoai = detail.TaiKhoan?.SoDienThoai ?? FormSoDienThoai;
            FormLoaiTaiKhoanId = detail.TaiKhoan?.LoaiTaiKhoanId?.ToString(CultureInfo.InvariantCulture) ?? FormLoaiTaiKhoanId;
            FormTenKhachHang = detail.KhachHang?.TenKh ?? FormTenKhachHang;
            FormDiaChi = detail.KhachHang?.DiaChi ?? string.Empty;
            FormGioiTinh = detail.KhachHang?.GioiTinh?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FormIdPhuong = detail.KhachHang?.IdPhuong?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FormIdQuan = detail.KhachHang?.IdQuan?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FormIdTinh = detail.KhachHang?.IdTinh?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            FormIdQuocTich = detail.KhachHang?.IdQuocTich?.ToString(CultureInfo.InvariantCulture) ?? FormIdQuocTich;
        }

        private static string? EmptyToNull(string? text)
            => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static int? ParseNullableInt(string? text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        public Task RefreshAsync() => LoadDanhSachAsync();

        private void OpenEditor()
        {
            IsEditorOpen = true;
        }

        private void CloseEditor()
        {
            IsEditorOpen = false;
        }

        private void RaiseCommandStates()
        {
            LoadDanhSachCommand.RaiseCanExecuteChanged();
            TaiChiTietCommand.RaiseCanExecuteChanged();
            LuuNguoiDungCommand.RaiseCanExecuteChanged();
            XoaNguoiDungCommand.RaiseCanExecuteChanged();
        }
    }
}





