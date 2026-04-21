using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Api.Admin;
using UI_Dat_Ve_May_Bay.Core;

namespace UI_Dat_Ve_May_Bay.ViewModels.Admin
{
    public class AdminMainViewModel : ObservableObject
    {
        private readonly ApiClient _apiClient;
        private readonly Action _logoutAction;
        private CancellationTokenSource? _toastCts;
        private INotifyPropertyChanged? _currentNotifyingViewModel;

        private QuanLyVeViewModel? _quanLyVeVM;
        private QuanLyPhieuGiamGiaViewModel? _quanLyPhieuGiamGiaVM;
        private QuanLyLichSuThanhToanViewModel? _quanLyLichSuThanhToanVM;
        private QuanLyChuyenBayViewModel? _quanLyChuyenBayVM;
        private QuanLyDoanhThuViewModel? _quanLyDoanhThuVM;
        private QuanLyNguoiDungViewModel? _quanLyNguoiDungVM;
        private QuanLyThongKeViewModel? _quanLyThongKeVM;

        public AdminMainViewModel(ApiClient apiClient, Action logoutAction)
        {
            _apiClient = apiClient;
            _logoutAction = logoutAction;

            GoQuanLyVeCommand = new RelayCommand(NavigateQuanLyVe);
            GoQuanLyPhieuGiamGiaCommand = new RelayCommand(NavigateQuanLyPhieuGiamGia);
            GoQuanLyLichSuThanhToanCommand = new RelayCommand(NavigateQuanLyLichSuThanhToan);
            GoQuanLyChuyenBayCommand = new RelayCommand(NavigateQuanLyChuyenBay);
            GoQuanLyDoanhThuCommand = new RelayCommand(NavigateQuanLyDoanhThu);
            GoQuanLyNguoiDungCommand = new RelayCommand(NavigateQuanLyNguoiDung);
            GoQuanLyThongKeCommand = new RelayCommand(NavigateQuanLyThongKe);
            RefreshCurrentCommand = new AsyncRelayCommand(RefreshCurrentAsync, () => CurrentViewModel is IAdminRefreshable);
            LogoutCommand = new RelayCommand(() => _logoutAction.Invoke());

            AdminUiService.ToastRequested += OnToastRequested;
            TodayText = DateTime.Now.ToString("dd/MM/yyyy");
            NavigateQuanLyVe();
        }

        private object _currentViewModel = new object();
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (SetProperty(ref _currentViewModel, value))
                {
                    AttachCurrentViewModelListener(value);
                    OnPropertyChanged(nameof(IsCurrentViewBusy));
                    RefreshCurrentCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsCurrentViewBusy
        {
            get
            {
                var property = CurrentViewModel?.GetType().GetProperty("IsBusy");
                return property?.GetValue(CurrentViewModel) as bool? ?? false;
            }
        }

        private string _currentTitle = "Quản lý vé";
        public string CurrentTitle
        {
            get => _currentTitle;
            set => SetProperty(ref _currentTitle, value);
        }

        private string _currentSubtitle = "Tra cứu vé, xem chi tiết và cập nhật thao tác điều hành.";
        public string CurrentSubtitle
        {
            get => _currentSubtitle;
            set => SetProperty(ref _currentSubtitle, value);
        }

        private string _currentIconGlyph = "\uE8AB";
        public string CurrentIconGlyph
        {
            get => _currentIconGlyph;
            set => SetProperty(ref _currentIconGlyph, value);
        }

        private string _currentSectionKey = "ve";
        public string CurrentSectionKey
        {
            get => _currentSectionKey;
            set => SetProperty(ref _currentSectionKey, value);
        }

        private string _todayText = string.Empty;
        public string TodayText
        {
            get => _todayText;
            set => SetProperty(ref _todayText, value);
        }

        private string _toastMessage = string.Empty;
        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        private bool _isToastVisible;
        public bool IsToastVisible
        {
            get => _isToastVisible;
            set => SetProperty(ref _isToastVisible, value);
        }

        private Brush _toastBackground = new SolidColorBrush(Color.FromRgb(15, 108, 189));
        public Brush ToastBackground
        {
            get => _toastBackground;
            set => SetProperty(ref _toastBackground, value);
        }

        public RelayCommand GoQuanLyVeCommand { get; }
        public RelayCommand GoQuanLyPhieuGiamGiaCommand { get; }
        public RelayCommand GoQuanLyLichSuThanhToanCommand { get; }
        public RelayCommand GoQuanLyChuyenBayCommand { get; }
        public RelayCommand GoQuanLyDoanhThuCommand { get; }
        public RelayCommand GoQuanLyNguoiDungCommand { get; }
        public RelayCommand GoQuanLyThongKeCommand { get; }
        public AsyncRelayCommand RefreshCurrentCommand { get; }
        public RelayCommand LogoutCommand { get; }

        private void NavigateQuanLyVe()
        {
            _quanLyVeVM ??= new QuanLyVeViewModel(new QuanLyVeApi(_apiClient));
            SetSection("ve", "Quản lý vé", "Theo dõi đặt vé, trạng thái và file PDF theo từng booking.", "\uE8AB", _quanLyVeVM);
        }

        private void NavigateQuanLyPhieuGiamGia()
        {
            _quanLyPhieuGiamGiaVM ??= new QuanLyPhieuGiamGiaViewModel(new QuanLyPhieuGiamGiaApi(_apiClient));
            SetSection("phieu-giam-gia", "Quản lý phiếu giảm giá", "Kiểm soát mã giảm, trạng thái kích hoạt và thời hạn hiệu lực.", "\uE8F1", _quanLyPhieuGiamGiaVM);
        }

        private void NavigateQuanLyLichSuThanhToan()
        {
            _quanLyLichSuThanhToanVM ??= new QuanLyLichSuThanhToanViewModel(new QuanLyLichSuThanhToanApi(_apiClient));
            SetSection("lich-su-thanh-toan", "Lịch sử thanh toán", "Kiểm tra giao dịch, lọc theo tiền và xuất báo cáo nhanh.", "\uE9D9", _quanLyLichSuThanhToanVM);
        }

        private void NavigateQuanLyChuyenBay()
        {
            _quanLyChuyenBayVM ??= new QuanLyChuyenBayViewModel(new QuanLyChuyenBayApi(_apiClient));
            SetSection("chuyen-bay", "Quản lý chuyến bay", "Điều phối tuyến, lịch bay hôm nay và thông tin tiện nghi.", "\uE804", _quanLyChuyenBayVM);
        }

        private void NavigateQuanLyDoanhThu()
        {
            _quanLyDoanhThuVM ??= new QuanLyDoanhThuViewModel(new QuanLyDoanhThuApi(_apiClient));
            SetSection("doanh-thu", "Quản lý doanh thu", "Xem nhanh doanh thu theo ngày, tháng và năm cùng biểu đồ so sánh.", "\uE9D2", _quanLyDoanhThuVM);
        }

        private void NavigateQuanLyNguoiDung()
        {
            _quanLyNguoiDungVM ??= new QuanLyNguoiDungViewModel(new QuanLyNguoiDungApi(_apiClient));
            SetSection("nguoi-dung", "Quản lý người dùng", "Tra cứu tài khoản, hồ sơ định danh và lịch sử giao dịch liên quan.", "\uE716", _quanLyNguoiDungVM);
        }

        private void NavigateQuanLyThongKe()
        {
            _quanLyThongKeVM ??= new QuanLyThongKeViewModel(new QuanLyThongKeApi(_apiClient));
            SetSection("thong-ke", "Quản lý thống kê", "Dashboard tổng hợp KPI, doanh thu range và biến động bán vé.", "\uE9D2", _quanLyThongKeVM);
        }

        private void SetSection(string key, string title, string subtitle, string glyph, object viewModel)
        {
            CurrentSectionKey = key;
            CurrentTitle = title;
            CurrentSubtitle = subtitle;
            CurrentIconGlyph = glyph;
            CurrentViewModel = viewModel;
        }

        private void AttachCurrentViewModelListener(object viewModel)
        {
            if (_currentNotifyingViewModel != null)
                _currentNotifyingViewModel.PropertyChanged -= OnCurrentViewModelPropertyChanged;

            _currentNotifyingViewModel = viewModel as INotifyPropertyChanged;
            if (_currentNotifyingViewModel != null)
                _currentNotifyingViewModel.PropertyChanged += OnCurrentViewModelPropertyChanged;
        }

        private void OnCurrentViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, "IsBusy", StringComparison.Ordinal))
                OnPropertyChanged(nameof(IsCurrentViewBusy));
        }

        private async Task RefreshCurrentAsync()
        {
            if (CurrentViewModel is not IAdminRefreshable refreshable) return;
            await refreshable.RefreshAsync();
            OnPropertyChanged(nameof(IsCurrentViewBusy));
        }

        private async void OnToastRequested(AdminToastMessage toast)
        {
            _toastCts?.Cancel();
            _toastCts = new CancellationTokenSource();
            var token = _toastCts.Token;

            ToastMessage = toast.Message;
            ToastBackground = toast.Type switch
            {
                "error" => new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                "warning" => new SolidColorBrush(Color.FromRgb(180, 83, 9)),
                _ => new SolidColorBrush(Color.FromRgb(15, 108, 189))
            };
            IsToastVisible = true;

            try
            {
                await Task.Delay(2800, token);
                if (!token.IsCancellationRequested)
                    IsToastVisible = false;
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
