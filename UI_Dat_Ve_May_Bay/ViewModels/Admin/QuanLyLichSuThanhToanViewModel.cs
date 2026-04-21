using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
    public class QuanLyLichSuThanhToanViewModel : ObservableObject, IAdminRefreshable
    {
        private readonly QuanLyLichSuThanhToanApi _api;

        public ObservableCollection<QuanLyLichSuThanhToanItemDto> DanhSachLichSuThanhToan { get; } = new();
        public ObservableCollection<QuanLyLichSuThanhToanThongKeItemDto> ThongKeTheoPhuongThuc { get; } = new();
        public ObservableCollection<QuanLyLichSuThanhToanThongKeItemDto> ThongKeTheoTrangThai { get; } = new();

        public QuanLyLichSuThanhToanViewModel(QuanLyLichSuThanhToanApi api)
        {
            _api = api;

            LoadDanhSachCommand = new AsyncRelayCommand(LoadDashboardAsync, () => !IsBusy);
            XemChiTietCommand = new AsyncRelayCommand(XemChiTietAsync, () => !IsBusy && SelectedLichSuThanhToan != null);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, () => !IsBusy);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsBusy && Page > 1);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsBusy && Page < TotalPages);
            ResetBoLocCommand = new RelayCommand(ResetBoLoc);

            NgayThanhToanFrom = DateTime.Today.AddDays(-7);
            NgayThanhToanTo = DateTime.Today;
            _ = LoadDashboardAsync();
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

        private DateTime? _ngayThanhToanFrom;
        public DateTime? NgayThanhToanFrom
        {
            get => _ngayThanhToanFrom;
            set => SetProperty(ref _ngayThanhToanFrom, value);
        }

        private DateTime? _ngayThanhToanTo;
        public DateTime? NgayThanhToanTo
        {
            get => _ngayThanhToanTo;
            set => SetProperty(ref _ngayThanhToanTo, value);
        }

        private string _soTienMin = string.Empty;
        public string SoTienMin
        {
            get => _soTienMin;
            set => SetProperty(ref _soTienMin, value);
        }

        private string _soTienMax = string.Empty;
        public string SoTienMax
        {
            get => _soTienMax;
            set => SetProperty(ref _soTienMax, value);
        }

        private int _page = 1;
        public int Page
        {
            get => _page;
            set
            {
                if (SetProperty(ref _page, value))
                    RaiseCommandStates();
            }
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                    RaiseCommandStates();
            }
        }

        private int _tongSoGiaoDich;
        public int TongSoGiaoDich
        {
            get => _tongSoGiaoDich;
            set => SetProperty(ref _tongSoGiaoDich, value);
        }

        private decimal _tongTien;
        public decimal TongTien
        {
            get => _tongTien;
            set
            {
                if (SetProperty(ref _tongTien, value))
                    OnPropertyChanged(nameof(TongTienText));
            }
        }

        public string TongTienText => $"{TongTien:N0} VND";

        private decimal _trungBinhGiaTri;
        public decimal TrungBinhGiaTri
        {
            get => _trungBinhGiaTri;
            set
            {
                if (SetProperty(ref _trungBinhGiaTri, value))
                    OnPropertyChanged(nameof(TrungBinhGiaTriText));
            }
        }

        public string TrungBinhGiaTriText => $"{TrungBinhGiaTri:N0} VND";

        private int _giaoDichThanhCong;
        public int GiaoDichThanhCong
        {
            get => _giaoDichThanhCong;
            set => SetProperty(ref _giaoDichThanhCong, value);
        }

        private int _giaoDichThatBai;
        public int GiaoDichThatBai
        {
            get => _giaoDichThatBai;
            set => SetProperty(ref _giaoDichThatBai, value);
        }

        public string FilterBadgeText => $"{TongSoGiaoDich} giao dịch";
        public Visibility FilterBadgeVisibility => TongSoGiaoDich > 0 ? Visibility.Visible : Visibility.Collapsed;
        public string PagingText => $"{Page}/{Math.Max(TotalPages, 1)}";

        private QuanLyLichSuThanhToanItemDto? _selectedLichSuThanhToan;
        public QuanLyLichSuThanhToanItemDto? SelectedLichSuThanhToan
        {
            get => _selectedLichSuThanhToan;
            set
            {
                if (SetProperty(ref _selectedLichSuThanhToan, value))
                {
                    RaiseCommandStates();
                    OnPropertyChanged(nameof(SelectedTransactionLabel));
                    OnPropertyChanged(nameof(SelectedCustomerText));
                    OnPropertyChanged(nameof(SelectedStatusText));
                    OnPropertyChanged(nameof(SelectedStatusBackground));
                    OnPropertyChanged(nameof(SelectedStatusForeground));
                    if (value != null)
                        _ = XemChiTietAsync();
                }
            }
        }

        private QuanLyLichSuThanhToanDetailDto? _chiTietLichSuThanhToan;
        public QuanLyLichSuThanhToanDetailDto? ChiTietLichSuThanhToan
        {
            get => _chiTietLichSuThanhToan;
            set
            {
                if (SetProperty(ref _chiTietLichSuThanhToan, value))
                {
                    OnPropertyChanged(nameof(SelectedTransactionLabel));
                    OnPropertyChanged(nameof(SelectedCustomerText));
                    OnPropertyChanged(nameof(SelectedStatusText));
                    OnPropertyChanged(nameof(SelectedStatusBackground));
                    OnPropertyChanged(nameof(SelectedStatusForeground));
                }
            }
        }

        public string SelectedTransactionLabel => ChiTietLichSuThanhToan != null
            ? $"GD #{ChiTietLichSuThanhToan.Id}"
            : SelectedLichSuThanhToan != null ? $"GD #{SelectedLichSuThanhToan.Id}" : "Chưa chọn giao dịch";

        public string SelectedCustomerText => ChiTietLichSuThanhToan?.TaiKhoan?.TenKhachHang
            ?? ChiTietLichSuThanhToan?.TaiKhoan?.Email
            ?? SelectedLichSuThanhToan?.KhachHangText
            ?? "--";

        public string SelectedStatusText => ChiTietLichSuThanhToan?.TrangThai?.TenTrangThai
            ?? SelectedLichSuThanhToan?.TrangThai
            ?? "Chưa có trạng thái";

        public Brush SelectedStatusBackground => GetStatusBackground(SelectedStatusText);
        public Brush SelectedStatusForeground => GetStatusForeground(SelectedStatusText);

        public AsyncRelayCommand LoadDanhSachCommand { get; }
        public AsyncRelayCommand XemChiTietCommand { get; }
        public AsyncRelayCommand ExportExcelCommand { get; }
        public AsyncRelayCommand PreviousPageCommand { get; }
        public AsyncRelayCommand NextPageCommand { get; }
        public RelayCommand ResetBoLocCommand { get; }

        private async Task LoadDashboardAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var filter = BuildFilter();
                var listTask = _api.GetDanhSachLichSuThanhToanAsync(filter, Page, PageSize);
                var tongQuanTask = _api.ThongKeTongQuanAsync(NgayThanhToanFrom, NgayThanhToanTo);
                var phuongThucTask = _api.ThongKeTheoPhuongThucThanhToanAsync(NgayThanhToanFrom, NgayThanhToanTo);
                var trangThaiTask = _api.ThongKeTheoTrangThaiAsync(NgayThanhToanFrom, NgayThanhToanTo);

                await Task.WhenAll(listTask, tongQuanTask, phuongThucTask, trangThaiTask);

                var paged = await listTask;
                var tongQuan = await tongQuanTask;
                var byMethod = await phuongThucTask;
                var byStatus = await trangThaiTask;

                DanhSachLichSuThanhToan.Clear();
                foreach (var item in paged.Items)
                    DanhSachLichSuThanhToan.Add(item);

                ThongKeTheoPhuongThuc.Clear();
                foreach (var item in byMethod)
                    ThongKeTheoPhuongThuc.Add(item);

                ThongKeTheoTrangThai.Clear();
                foreach (var item in byStatus)
                    ThongKeTheoTrangThai.Add(item);

                TotalPages = Math.Max(paged.Pagination.TotalPages, 1);
                TongSoGiaoDich = tongQuan?.TongSoGiaoDich ?? paged.Pagination.TotalCount;
                TongTien = tongQuan?.TongTien ?? 0;
                TrungBinhGiaTri = tongQuan?.TrungBinhGiaTri ?? 0;
                GiaoDichThanhCong = tongQuan?.GiaoDichThanhCong ?? 0;
                GiaoDichThatBai = tongQuan?.GiaoDichThatBai ?? 0;

                if (DanhSachLichSuThanhToan.Count == 0)
                {
                    SelectedLichSuThanhToan = null;
                    ChiTietLichSuThanhToan = null;
                    StatusMessage = "Không có lịch sử thanh toán phù hợp.";
                }
                else
                {
                    SelectedLichSuThanhToan ??= DanhSachLichSuThanhToan.FirstOrDefault();
                    StatusMessage = paged.Message ?? $"Đã tải {DanhSachLichSuThanhToan.Count} giao dịch.";
                }

                OnPropertyChanged(nameof(FilterBadgeText));
                OnPropertyChanged(nameof(FilterBadgeVisibility));
                OnPropertyChanged(nameof(PagingText));
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải lịch sử thanh toán: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task XemChiTietAsync()
        {
            if (SelectedLichSuThanhToan == null) return;

            try
            {
                ChiTietLichSuThanhToan = await _api.GetChiTietLichSuThanhToanAsync(SelectedLichSuThanhToan.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải chi tiết giao dịch: " + ex.Message;
            }
        }

        private async Task ExportExcelAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var file = await _api.ExportExcelAsync(BuildFilter());
                var dialog = new SaveFileDialog
                {
                    FileName = file.FileName,
                    Filter = "Excel (*.xlsx)|*.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, file.Content);
                    StatusMessage = "Đã xuất file Excel thành công.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi xuất Excel: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PreviousPageAsync()
        {
            if (Page <= 1) return;
            Page--;
            await LoadDashboardAsync();
        }

        private async Task NextPageAsync()
        {
            if (Page >= TotalPages) return;
            Page++;
            await LoadDashboardAsync();
        }

        private void ResetBoLoc()
        {
            NgayThanhToanFrom = DateTime.Today.AddDays(-7);
            NgayThanhToanTo = DateTime.Today;
            SoTienMin = string.Empty;
            SoTienMax = string.Empty;
            Page = 1;
            StatusMessage = string.Empty;
        }

        private QuanLyLichSuThanhToanFilterModel BuildFilter()
        {
            return new QuanLyLichSuThanhToanFilterModel
            {
                NgayThanhToanFrom = NgayThanhToanFrom,
                NgayThanhToanTo = NgayThanhToanTo,
                SoTienMin = ParseDecimal(SoTienMin),
                SoTienMax = ParseDecimal(SoTienMax)
            };
        }

        private static decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out value)
                    ? value
                    : null;
        }

        public Task RefreshAsync() => LoadDashboardAsync();

        private void RaiseCommandStates()
        {
            LoadDanhSachCommand.RaiseCanExecuteChanged();
            XemChiTietCommand.RaiseCanExecuteChanged();
            ExportExcelCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
        }

        private static Brush GetStatusBackground(string? status)
        {
            var normalized = (status ?? string.Empty).ToLowerInvariant();
            if (normalized.Contains("thanh cong") || normalized.Contains("thanhcong") || normalized.Contains("success"))
                return new SolidColorBrush(Color.FromRgb(220, 252, 231));
            if (normalized.Contains("that bai") || normalized.Contains("thatbai") || normalized.Contains("fail"))
                return new SolidColorBrush(Color.FromRgb(254, 226, 226));
            return new SolidColorBrush(Color.FromRgb(219, 234, 254));
        }

        private static Brush GetStatusForeground(string? status)
        {
            var normalized = (status ?? string.Empty).ToLowerInvariant();
            if (normalized.Contains("thanh cong") || normalized.Contains("thanhcong") || normalized.Contains("success"))
                return new SolidColorBrush(Color.FromRgb(22, 101, 52));
            if (normalized.Contains("that bai") || normalized.Contains("thatbai") || normalized.Contains("fail"))
                return new SolidColorBrush(Color.FromRgb(153, 27, 27));
            return new SolidColorBrush(Color.FromRgb(30, 64, 175));
        }
    }
}



