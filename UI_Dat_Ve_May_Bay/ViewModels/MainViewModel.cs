using System.Windows;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Api.Admin;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Services;
using UI_Dat_Ve_May_Bay.ViewModels.Admin;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
            : this(new ApiClient("https://localhost:7242"), new TokenStore())
        { }

        private readonly ApiClient _apiClient;
        private readonly TokenStore _tokenStore;

        private HomeViewModel? _homeVM;
        private MyFlightsViewModel? _myFlightsVM;
        private NotificationViewModel? _notiVM;
        private VoucherViewModel? _voucherVM;
        private AuthViewModel? _authVM;
        private ProfileViewModel? _profileVM;
        private AiChatViewModel? _aiChatVM;
        private AdminMainViewModel? _adminMainVM;

        private FlightViewModel? _flightVM;
        private BookingViewModel? _bookingVM;

        private object _currentViewModel = new object();
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (SetProperty(ref _currentViewModel, value))
                {
                    OnPropertyChanged(nameof(IsAppScreen));
                    OnPropertyChanged(nameof(IsUserShell));
                    OnPropertyChanged(nameof(IsAdminShell));
                }
            }
        }

        private string _currentTabName = "Home";
        public string CurrentTabName { get => _currentTabName; set => SetProperty(ref _currentTabName, value); }

        public bool IsAppScreen => _authVM != null && CurrentViewModel != _authVM;
        public bool IsUserShell => IsAppScreen && CurrentViewModel is not AdminMainViewModel;
        public bool IsAdminShell => CurrentViewModel is AdminMainViewModel;

        public RelayCommand GoHomeCommand { get; }
        public RelayCommand GoNotificationCommand { get; }
        public RelayCommand GoVoucherCommand { get; }
        public RelayCommand GoFlightCommand { get; }
        public RelayCommand GoMyFlightsCommand { get; }
        public RelayCommand GoProfileCommand { get; }
        public RelayCommand GoAiChatCommand { get; }

        public RelayCommand LogoutCommand { get; }
        public RelayCommand ClearTokenCommand { get; }
        public RelayCommand ShowTokenPathCommand { get; }

        public MainViewModel(ApiClient apiClient, TokenStore tokenStore)
        {
            _apiClient = apiClient;
            _tokenStore = tokenStore;

            CurrentTabName = "Đăng nhập";
            CurrentViewModel = new object();

            try
            {
                ReloadTokenToApiClient();

                // ✅ FIX: HomeViewModel cần ApiClient để gọi Huỷ vé / Check-in
                _homeVM = new HomeViewModel(_apiClient);

                // IMPORTANT: AuthViewModel phải nhận đúng ApiClient đang dùng trong app.
                // Nếu dùng nhầm overload không truyền ApiClient, các Command sẽ không được init => bấm đăng nhập không chạy.
                _authVM = new AuthViewModel(new AuthApi(_apiClient), _tokenStore, _apiClient, onLoginSuccess: NavigateHome);

                if (!string.IsNullOrWhiteSpace(_apiClient.Token))
                {
                    NavigateAfterStartup();
                }
                else
                {
                    CurrentTabName = "Đăng nhập";
                    CurrentViewModel = _authVM;
                }
            }
            catch (System.Exception ex)
            {
                DialogService.ShowError(
                    $"Lỗi khởi tạo UI: {ex.Message}\n\nHệ thống sẽ chuyển về màn hình đăng nhập.",
                    "Init error");

                _authVM ??= new AuthViewModel(new AuthApi(_apiClient), _tokenStore, _apiClient, onLoginSuccess: NavigateHome);
                CurrentTabName = "Đăng nhập";
                CurrentViewModel = _authVM;
            }

            GoHomeCommand = new RelayCommand(NavigateHome);

            GoNotificationCommand = new RelayCommand(() =>
            {
                if (!EnsureLoggedIn()) return;

                _notiVM ??= new NotificationViewModel(new NotificationApi(_apiClient));
                CurrentTabName = "Thông báo";
                CurrentViewModel = _notiVM;
            });

            GoVoucherCommand = new RelayCommand(() =>
            {
                if (!EnsureLoggedIn()) return;
                CurrentTabName = "Voucher";

                _voucherVM ??= new VoucherViewModel(new VoucherApi(_apiClient), autoLoad: true);
                CurrentViewModel = _voucherVM;
            });

            GoFlightCommand = new RelayCommand(() =>
            {
                if (!EnsureLoggedIn()) return;

                _flightVM ??= new FlightViewModel(_apiClient, GoToBooking);
                CurrentTabName = "Chuyến bay";
                CurrentViewModel = _flightVM;
            });

            GoMyFlightsCommand = new RelayCommand(() =>
            {
                if (!EnsureLoggedIn()) return;

                _myFlightsVM ??= new MyFlightsViewModel(new DichVuApi(_apiClient), autoLoad: true);
                CurrentTabName = "Chuyến bay của bạn";
                CurrentViewModel = _myFlightsVM;
            });

            GoProfileCommand = new RelayCommand(() =>
            {
                if (!EnsureLoggedIn()) return;

                _profileVM ??= new ProfileViewModel(_apiClient);
                CurrentTabName = "Hồ sơ";
                CurrentViewModel = _profileVM;
            });

            GoAiChatCommand = new RelayCommand(() =>
            {
                if (!EnsureLoggedIn()) return;

                _aiChatVM ??= new AiChatViewModel();
                CurrentTabName = "Hoi dap voi AI";
                CurrentViewModel = _aiChatVM;
            });

            LogoutCommand = new RelayCommand(() =>
            {
                _tokenStore.Clear();
                _apiClient.Token = null;
                _notiVM = null;
                _voucherVM = null;
                _flightVM = null;
                _bookingVM = null;
                _myFlightsVM = null;
                _profileVM = null;
                _aiChatVM = null;
                _adminMainVM = null;

                CurrentTabName = "Đăng nhập";
                CurrentViewModel = _authVM ?? new object();

                DialogService.ShowSuccess("Đã đăng xuất thành công.", "Đăng xuất");
            });

            ClearTokenCommand = new RelayCommand(() =>
            {
                _tokenStore.Clear();
                _apiClient.Token = null;
                _notiVM = null;
                _voucherVM = null;
                _flightVM = null;
                _bookingVM = null;
                _myFlightsVM = null;
                _profileVM = null;
                _aiChatVM = null;
                _adminMainVM = null;

                DialogService.ShowInfo("Đã xóa token. App sẽ quay về đăng nhập.", "Token cleared");

                CurrentTabName = "Đăng nhập";
                CurrentViewModel = _authVM ?? new object();
            });

            ShowTokenPathCommand = new RelayCommand(() =>
            {
                DialogService.ShowInfo(_tokenStore.GetFilePath(), "Token file path");
            });
        }

        private void NavigateFlight()
        {
            if (!EnsureLoggedIn()) return;

            _flightVM ??= new FlightViewModel(_apiClient, GoToBooking);
            CurrentTabName = "Chuyến bay";
            CurrentViewModel = _flightVM;
        }

        private void GoToBooking(FlightViewModel.LichBayItemVm? selected)
        {
            if (selected is null) return;

            // ✅ REUSE: Nếu đang có booking cho đúng chuyến bay này thì không tạo mới
            if (_bookingVM != null && _bookingVM.SelectedSchedule.Id == selected.Id)
            {
                CurrentTabName = "Đặt vé";
                CurrentViewModel = _bookingVM;
                return;
            }

            _bookingVM = new BookingViewModel(
                _apiClient,
                selected,
                () => {
                    _bookingVM = null; // Xoá session khi nhấn Quay lại
                    // ✅ FIX: Không tạo FlightViewModel mới, giữ lại trạng thái cũ
                    if (_flightVM != null)
                    {
                        CurrentTabName = "Chuyến bay";
                        CurrentViewModel = _flightVM;
                    }
                    else
                    {
                        NavigateFlight();
                    }
                }
            );

            CurrentTabName = "Đặt vé";
            CurrentViewModel = _bookingVM;

            if (_bookingVM.RefreshSeatsCommand.CanExecute(null))
                _bookingVM.RefreshSeatsCommand.Execute(null);
        }

        private void ReloadTokenToApiClient()
        {
            var token = _tokenStore.Load();
            _apiClient.Token = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        }

        private void NavigateHome()
        {
            // Reset all sub-VMs before re-init for new session
            _flightVM = null;
            _bookingVM = null;
            _myFlightsVM = null;
            _notiVM = null;
            _voucherVM = null;
            _profileVM = null;
            _aiChatVM = null;
            _adminMainVM = null;

            ReloadTokenToApiClient();

            if (!EnsureLoggedIn(showMessage: false))
            {
                CurrentTabName = "Đăng nhập";
                CurrentViewModel = _authVM ?? new object();
                return;
            }

            NavigateAfterLogin();
        }

        private void NavigateAfterLogin()
        {
            var loaiTaiKhoan = _tokenStore.LoadAccountType();

            if (loaiTaiKhoan == 3)
            {
                _adminMainVM ??= new AdminMainViewModel(_apiClient, PerformLogout);
                CurrentTabName = "Admin";
                CurrentViewModel = _adminMainVM;
                return;
            }

            CurrentTabName = "Home";
            CurrentViewModel = _homeVM ?? new object();
        }

        private void NavigateAfterStartup()
        {
            var loaiTaiKhoan = _tokenStore.LoadAccountType();

            if (loaiTaiKhoan == 3)
            {
                _adminMainVM ??= new AdminMainViewModel(_apiClient, PerformLogout);
                CurrentTabName = "Admin";
                CurrentViewModel = _adminMainVM;
                return;
            }

            CurrentTabName = "Home";
            CurrentViewModel = _homeVM ?? new object();
        }

        private void PerformLogout()
        {
            _tokenStore.Clear();
            _apiClient.Token = null;
            _notiVM = null;
            _voucherVM = null;
            _flightVM = null;
            _bookingVM = null;
            _profileVM = null;
            _aiChatVM = null;
            _adminMainVM = null;

            CurrentTabName = "Đăng nhập";
            CurrentViewModel = _authVM ?? new object();

            DialogService.ShowSuccess("Đã đăng xuất thành công.", "Đăng xuất");
        }

        private bool EnsureLoggedIn(bool showMessage = true)
        {
            // phòng trường hợp token đã được lưu ở TokenStore nhưng _apiClient.Token chưa được set
            if (string.IsNullOrWhiteSpace(_apiClient.Token))
                ReloadTokenToApiClient();

            if (!string.IsNullOrWhiteSpace(_apiClient.Token))
                return true;

            if (showMessage)
            {
                DialogService.ShowWarning(
                    "Bạn chưa đăng nhập (chưa có token). Hệ thống sẽ chuyển về đăng nhập.",
                    "Thiếu token"
                );
            }

            CurrentTabName = "Đăng nhập";
            CurrentViewModel = _authVM ?? new object();
            return false;
        }
    }
}
