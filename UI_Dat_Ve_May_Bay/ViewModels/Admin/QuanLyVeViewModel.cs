using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using UI_Dat_Ve_May_Bay.Api.Admin;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Models.Admin;

namespace UI_Dat_Ve_May_Bay.ViewModels.Admin
{
    public class QuanLyVeViewModel : ObservableObject, IAdminRefreshable
    {
        private readonly QuanLyVeApi _api;

        public ObservableCollection<QuanLyVeDanhSachItemDto> DanhSachDatVe { get; } = new();
        public ObservableCollection<QuanLyVeChiTietGheDto> DanhSachGhe { get; } = new();
        public ObservableCollection<string> TrangThaiOptions { get; } = new(new[]
        {
            "",
            "đã đặt",
            "đã checkin",
            "đã hủy"
        });

        public QuanLyVeViewModel(QuanLyVeApi api)
        {
            _api = api;

            DanhSachDatVe.CollectionChanged += OnCollectionsChanged;
            DanhSachGhe.CollectionChanged += OnCollectionsChanged;

            LoadDanhSachCommand = new AsyncRelayCommand(LoadDanhSachAsync, () => !IsBusy);
            XemChiTietCommand = new AsyncRelayCommand(XemChiTietAsync, () => !IsBusy && SelectedDatVe != null);
            CapNhatTrangThaiCommand = new AsyncRelayCommand(CapNhatTrangThaiAsync, () => !IsBusy && SelectedDatVe != null && !string.IsNullOrWhiteSpace(TrangThaiMoi));
            InChiTietVeCommand = new AsyncRelayCommand(InChiTietVeAsync, () => !IsBusy && SelectedDatVe != null);
            ResetBoLocCommand = new RelayCommand(ResetBoLoc);

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

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    AdminUiService.Publish(value);
                    RaiseFeedbackProperties();
                }
            }
        }

        public int TongSoVe => DanhSachDatVe.Count;

        public string FilterBadgeText => $"\u0110\u00E3 t\u1EA3i {TongSoVe} v\u00E9";

        public Visibility FilterBadgeVisibility
            => TongSoVe > 0 ? Visibility.Visible : Visibility.Collapsed;

        public string InlineFeedbackText
            => ShouldShowInlineFeedback(StatusMessage) ? StatusMessage : string.Empty;

        public Visibility InlineFeedbackVisibility
            => string.IsNullOrWhiteSpace(InlineFeedbackText) ? Visibility.Collapsed : Visibility.Visible;

        public string SelectedTicketLabel
            => SelectedDatVe != null ? $"V\u00E9 #{SelectedDatVe.Id}" : "Ch\u01B0a ch\u1ECDn v\u00E9";

        public string SelectedTrangThaiText
            => ChiTietDatVe?.TrangThai
            ?? SelectedDatVe?.TrangThai
            ?? "Ch\u01B0a c\u00F3 tr\u1EA1ng th\u00E1i";

        public string SelectedHanhTrinhText
            => ChiTietDatVe != null
                ? $"{ChiTietDatVe.SanBayDi ?? "--"} -> {ChiTietDatVe.SanBayDen ?? "--"}"
                : SelectedDatVe?.HanhTrinhText ?? "Ch\u01B0a c\u00F3 h\u00E0nh tr\u00ECnh";

        public string SelectedKhachHangText
            => ChiTietDatVe?.Email
            ?? SelectedDatVe?.Email
            ?? "Ch\u01B0a ch\u1ECDn h\u00E0nh kh\u00E1ch";

        public string SelectedSanBayDiText
            => ChiTietDatVe?.SanBayDi
            ?? SelectedDatVe?.MaSanBayDi
            ?? "--";

        public string SelectedSanBayDenText
            => ChiTietDatVe?.SanBayDen
            ?? SelectedDatVe?.MaSanBayDen
            ?? "--";

        public Brush SelectedStatusBackground
            => GetStatusBackgroundBrush(SelectedTrangThaiText);

        public Brush SelectedStatusForeground
            => GetStatusForegroundBrush(SelectedTrangThaiText);

        private QuanLyVeDanhSachItemDto? _selectedDatVe;
        public QuanLyVeDanhSachItemDto? SelectedDatVe
        {
            get => _selectedDatVe;
            set
            {
                if (SetProperty(ref _selectedDatVe, value))
                {
                    RaiseDisplayProperties();
                    RaiseCommandStates();
                    if (value != null)
                        _ = XemChiTietAsync();
                }
            }
        }

        private QuanLyVeChiTietDto? _chiTietDatVe;
        public QuanLyVeChiTietDto? ChiTietDatVe
        {
            get => _chiTietDatVe;
            set
            {
                if (SetProperty(ref _chiTietDatVe, value))
                    RaiseDisplayProperties();
            }
        }

        private string _maDatVe = "";
        public string MaDatVe
        {
            get => _maDatVe;
            set => SetProperty(ref _maDatVe, value);
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _soDienThoai = "";
        public string SoDienThoai
        {
            get => _soDienThoai;
            set => SetProperty(ref _soDienThoai, value);
        }

        private string _idChuyenBay = "";
        public string IdChuyenBay
        {
            get => _idChuyenBay;
            set => SetProperty(ref _idChuyenBay, value);
        }

        private string _idLichBay = "";
        public string IdLichBay
        {
            get => _idLichBay;
            set => SetProperty(ref _idLichBay, value);
        }

        private string _maSanBayDi = "";
        public string MaSanBayDi
        {
            get => _maSanBayDi;
            set => SetProperty(ref _maSanBayDi, value);
        }

        private string _maSanBayDen = "";
        public string MaSanBayDen
        {
            get => _maSanBayDen;
            set => SetProperty(ref _maSanBayDen, value);
        }

        private string _trangThai = "";
        public string TrangThai
        {
            get => _trangThai;
            set => SetProperty(ref _trangThai, value);
        }

        private DateTime? _ngayDatFrom;
        public DateTime? NgayDatFrom
        {
            get => _ngayDatFrom;
            set => SetProperty(ref _ngayDatFrom, value);
        }

        private DateTime? _ngayDatTo;
        public DateTime? NgayDatTo
        {
            get => _ngayDatTo;
            set => SetProperty(ref _ngayDatTo, value);
        }

        private string _trangThaiMoi = "";
        public string TrangThaiMoi
        {
            get => _trangThaiMoi;
            set
            {
                if (SetProperty(ref _trangThaiMoi, value))
                    RaiseCommandStates();
            }
        }

        public AsyncRelayCommand LoadDanhSachCommand { get; }
        public AsyncRelayCommand XemChiTietCommand { get; }
        public AsyncRelayCommand CapNhatTrangThaiCommand { get; }
        public AsyncRelayCommand InChiTietVeCommand { get; }
        public RelayCommand ResetBoLocCommand { get; }

        private async Task LoadDanhSachAsync()
        {
            StatusMessage = "";
            IsBusy = true;

            try
            {
                var data = await _api.GetDanhSachDatVeAsync(BuildFilter());

                DanhSachDatVe.Clear();
                foreach (var item in data)
                    DanhSachDatVe.Add(item);

                if (DanhSachDatVe.Count == 0)
                {
                    SelectedDatVe = null;
                    ChiTietDatVe = null;
                    DanhSachGhe.Clear();
                    StatusMessage = "Kh\u00F4ng c\u00F3 d\u1EEF li\u1EC7u \u0111\u1EB7t v\u00E9 ph\u00F9 h\u1EE3p.";
                    return;
                }

                SelectedDatVe ??= DanhSachDatVe.FirstOrDefault();
                StatusMessage = $"Đã tải {DanhSachDatVe.Count} vé.";
            }
            catch (Exception ex)
            {
                StatusMessage = "L\u1ED7i t\u1EA3i danh s\u00E1ch v\u00E9: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task XemChiTietAsync()
        {
            if (SelectedDatVe == null) return;

            StatusMessage = "";
            IsBusy = true;

            try
            {
                var detail = await _api.GetChiTietDatVeAsync(SelectedDatVe.Id);
                ChiTietDatVe = detail;
                TrangThaiMoi = detail?.TrangThai ?? SelectedDatVe.TrangThai ?? "";

                DanhSachGhe.Clear();
                foreach (var item in detail?.ChiTiet ?? new List<QuanLyVeChiTietGheDto>())
                    DanhSachGhe.Add(item);
            }
            catch (Exception ex)
            {
                StatusMessage = "L\u1ED7i l\u1EA5y chi ti\u1EBFt v\u00E9: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CapNhatTrangThaiAsync()
        {
            if (SelectedDatVe == null || string.IsNullOrWhiteSpace(TrangThaiMoi)) return;

            StatusMessage = "";
            IsBusy = true;

            try
            {
                var result = await _api.CapNhatTrangThaiAsync(new QuanLyVeCapNhatTrangThaiVeModel
                {
                    IdDatVe = SelectedDatVe.Id,
                    TrangThaiMoi = TrangThaiMoi.Trim()
                });

                StatusMessage = result.Message ?? "C\u1EADp nh\u1EADt tr\u1EA1ng th\u00E1i th\u00E0nh c\u00F4ng.";
                await LoadDanhSachAsync();

                var current = DanhSachDatVe.FirstOrDefault(x => x.Id == SelectedDatVe.Id);
                if (current != null)
                    SelectedDatVe = current;
            }
            catch (Exception ex)
            {
                StatusMessage = "L\u1ED7i c\u1EADp nh\u1EADt tr\u1EA1ng th\u00E1i: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task InChiTietVeAsync()
        {
            if (SelectedDatVe == null) return;

            StatusMessage = "";
            IsBusy = true;

            try
            {
                var pdfBytes = await _api.InChiTietVeAsync(SelectedDatVe.Id);
                var filePath = Path.Combine(Path.GetTempPath(), $"QuanLyVe_{SelectedDatVe.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                StatusMessage = $"\u0110\u00E3 t\u1EA1o file PDF t\u1EA1i {filePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = "L\u1ED7i in chi ti\u1EBFt v\u00E9: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetBoLoc()
        {
            MaDatVe = "";
            Email = "";
            SoDienThoai = "";
            IdChuyenBay = "";
            IdLichBay = "";
            MaSanBayDi = "";
            MaSanBayDen = "";
            TrangThai = "";
            NgayDatFrom = null;
            NgayDatTo = null;
            StatusMessage = "";

            _ = LoadDanhSachAsync();
        }

        private QuanLyVeLocDatVeAdminModel BuildFilter()
        {
            return new QuanLyVeLocDatVeAdminModel
            {
                MaDatVe = ParseNullableLong(MaDatVe),
                Email = NullIfWhiteSpace(Email),
                SoDienThoai = NullIfWhiteSpace(SoDienThoai),
                IdChuyenBay = ParseNullableLong(IdChuyenBay),
                IdLichBay = ParseNullableLong(IdLichBay),
                MaSanBayDi = NullIfWhiteSpace(MaSanBayDi),
                MaSanBayDen = NullIfWhiteSpace(MaSanBayDen),
                TrangThai = NullIfWhiteSpace(TrangThai),
                NgayDatFrom = NgayDatFrom,
                NgayDatTo = NgayDatTo
            };
        }

        private static long? ParseNullableLong(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return long.TryParse(text.Trim(), out var value) ? value : null;
        }

        private static string? NullIfWhiteSpace(string? text)
            => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        public Task RefreshAsync() => LoadDanhSachAsync();

        private void RaiseCommandStates()
        {
            LoadDanhSachCommand.RaiseCanExecuteChanged();
            XemChiTietCommand.RaiseCanExecuteChanged();
            CapNhatTrangThaiCommand.RaiseCanExecuteChanged();
            InChiTietVeCommand.RaiseCanExecuteChanged();
        }

        private void OnCollectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseDisplayProperties();
        }

        private void RaiseDisplayProperties()
        {
            OnPropertyChanged(nameof(TongSoVe));
            OnPropertyChanged(nameof(FilterBadgeText));
            OnPropertyChanged(nameof(FilterBadgeVisibility));
            OnPropertyChanged(nameof(SelectedTicketLabel));
            OnPropertyChanged(nameof(SelectedTrangThaiText));
            OnPropertyChanged(nameof(SelectedHanhTrinhText));
            OnPropertyChanged(nameof(SelectedKhachHangText));
            OnPropertyChanged(nameof(SelectedSanBayDiText));
            OnPropertyChanged(nameof(SelectedSanBayDenText));
            OnPropertyChanged(nameof(SelectedStatusBackground));
            OnPropertyChanged(nameof(SelectedStatusForeground));
        }

        private void RaiseFeedbackProperties()
        {
            OnPropertyChanged(nameof(InlineFeedbackText));
            OnPropertyChanged(nameof(InlineFeedbackVisibility));
        }

        private static Brush GetStatusBackgroundBrush(string? status)
        {
            var value = NormalizeStatus(status);

            if (value.Contains("hủy") || value.Contains("huy"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE8E8"));

            if (value.Contains("checkin"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));

            if (value.Contains("đặt") || value.Contains("dat"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAF2FF"));
        }

        private static Brush GetStatusForegroundBrush(string? status)
        {
            var value = NormalizeStatus(status);

            if (value.Contains("hủy") || value.Contains("huy"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B42318"));

            if (value.Contains("checkin"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C4A6E"));

            if (value.Contains("đặt") || value.Contains("dat"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F6CBD"));
        }

        private static string NormalizeStatus(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static bool ShouldShowInlineFeedback(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            return !message.Trim().StartsWith("\u0110\u00E3 t\u1EA3i", StringComparison.OrdinalIgnoreCase);
        }
    }
}



