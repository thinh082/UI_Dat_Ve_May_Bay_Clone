using System;

using System.Threading.Tasks;

using System.Windows;

using UI_Dat_Ve_May_Bay.Api;

using UI_Dat_Ve_May_Bay.Core;//

using UI_Dat_Ve_May_Bay.Services;
using UI_Dat_Ve_May_Bay.Views.Components;



namespace UI_Dat_Ve_May_Bay.ViewModels

{

    public enum ForgotStep

    {

        EnterEmail = 0,

        EnterOtp = 1,

        ResetPassword = 2

    }



    public class AuthViewModel : ObservableObject

    {

        private readonly AuthApi _authApi;

        private readonly TokenStore _tokenStore;

        private readonly ApiClient _apiClient;

        private readonly Action _onLoginSuccess;



        private readonly SocialAuthHubClient _hubClient;
        private SocialLoginWebViewWindow? _socialLoginWindow;



        // Hub BE: app.MapHub<NotificationHub>("/hubs/notify");

        private const string HubUrlHttps = "https://localhost:7242/hubs/notify";

        private const string HubUrlHttp = "http://localhost:5231/hubs/notify";



        public AuthViewModel(AuthApi authApi, TokenStore tokenStore, ApiClient apiClient, Action onLoginSuccess)

        {

            _authApi = authApi;

            _tokenStore = tokenStore;

            _apiClient = apiClient;

            _onLoginSuccess = onLoginSuccess;



            _hubClient = new SocialAuthHubClient();



            LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsBusy);

            RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !IsBusy);

            SendOtpCommand = new AsyncRelayCommand(SendOtpAsync, () => !IsBusy);

            VerifyOtpCommand = new AsyncRelayCommand(VerifyOtpAsync, () => !IsBusy);

            ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync, () => !IsBusy);



            // ✅ Social

            LoginGoogleCommand = new AsyncRelayCommand(LoginGoogleAsync, () => !IsBusy);

            LoginFacebookCommand = new AsyncRelayCommand(LoginFacebookAsync, () => !IsBusy);

            LoginZaloCommand = new AsyncRelayCommand(LoginZaloAsync, () => !IsBusy);



            ResetForgotFlow();

        }



        public AuthViewModel(AuthApi authApi, TokenStore tokenStore, Action onLoginSuccess)

            : this(

                  authApi,

                  tokenStore,

                  // Fallback an toàn: tạo ApiClient cùng BaseUrl mặc định.

                  // MainViewModel hiện đã truyền đúng ApiClient; overload này giữ lại để tránh lỗi khi ai đó lỡ dùng.

                  new ApiClient("https://localhost:7242"),

                  onLoginSuccess)

        {

        }



        // -------- Common UI state --------

        private bool _isBusy;

        public bool IsBusy

        {

            get => _isBusy;

            set

            {

                if (SetProperty(ref _isBusy, value))

                {

                    LoginCommand.RaiseCanExecuteChanged();

                    RegisterCommand.RaiseCanExecuteChanged();

                    SendOtpCommand.RaiseCanExecuteChanged();

                    VerifyOtpCommand.RaiseCanExecuteChanged();

                    ResetPasswordCommand.RaiseCanExecuteChanged();



                    LoginGoogleCommand.RaiseCanExecuteChanged();

                    LoginFacebookCommand.RaiseCanExecuteChanged();

                    LoginZaloCommand.RaiseCanExecuteChanged();

                }

            }

        }



        private string _statusMessage = "";

        public string StatusMessage

        {

            get => _statusMessage;

            set => SetProperty(ref _statusMessage, value);

        }



        // -------- Login --------

        private string _loginTaiKhoan = "";

        public string LoginTaiKhoan

        {

            get => _loginTaiKhoan;

            set => SetProperty(ref _loginTaiKhoan, value);

        }



        private string _loginMatKhau = "";

        public string LoginMatKhau

        {

            get => _loginMatKhau;

            set => SetProperty(ref _loginMatKhau, value);

        }



        public AsyncRelayCommand LoginCommand { get; }



        private async Task LoginAsync()

        {

            StatusMessage = "";

            IsBusy = true;

            try

            {

                var (ok, msg, token, loaiTaiKhoan) = await _authApi.LoginAsync(LoginTaiKhoan?.Trim() ?? "", LoginMatKhau ?? "");

                StatusMessage = msg;



                if (!ok || string.IsNullOrWhiteSpace(token))

                    return;



                _tokenStore.Save(token);

                _tokenStore.SaveAccountType(loaiTaiKhoan);

                _apiClient.Token = token;



                _onLoginSuccess.Invoke();

            }

            catch (Exception ex)

            {

                StatusMessage = "Lỗi đăng nhập: " + ex.Message;

            }

            finally

            {

                IsBusy = false;

            }

        }



        // -------- Social login --------

        public AsyncRelayCommand LoginGoogleCommand { get; }

        public AsyncRelayCommand LoginFacebookCommand { get; }

        public AsyncRelayCommand LoginZaloCommand { get; }



        private bool _hubHooked;



        private void ShowSocialLoginWindow(string url, SocialProvider provider)

        {

            void OpenWindow()

            {

                _socialLoginWindow?.Close();



                var window = new SocialLoginWebViewWindow(url, provider.ToString());

                if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)

                {

                    window.Owner = Application.Current.MainWindow;

                }



                window.Closed += (_, _) =>

                {

                    if (ReferenceEquals(_socialLoginWindow, window))

                    {

                        _socialLoginWindow = null;

                    }

                };



                _socialLoginWindow = window;

                window.Show();

                window.Activate();

            }



            if (Application.Current.Dispatcher.CheckAccess())

            {

                OpenWindow();

                return;

            }



            Application.Current.Dispatcher.Invoke(OpenWindow);

        }



        private void CloseSocialLoginWindow()

        {

            void CloseWindow()

            {

                if (_socialLoginWindow == null) return;

                _socialLoginWindow.Close();

                _socialLoginWindow = null;

            }



            if (Application.Current.Dispatcher.CheckAccess())

            {

                CloseWindow();

                return;

            }



            Application.Current.Dispatcher.Invoke(CloseWindow);

        }



        private async Task EnsureHubConnectedAsync()

        {

            if (_hubHooked) return;



            // thử https trước (BE có UseHttpsRedirection), fail thì fallback http

            try

            {

                await _hubClient.ConnectAsync(HubUrlHttps);

            }

            catch

            {

                await _hubClient.ConnectAsync(HubUrlHttp);

            }



            _hubClient.OnGoogleResult(OnSocialTokenReceived);

            _hubClient.OnFacebookResult(OnSocialTokenReceived);

            _hubClient.OnZaloResult(OnSocialTokenReceived);



            _hubHooked = true;

        }



        private void OnSocialTokenReceived(string? token)

        {

            // callback từ SignalR có thể không ở UI thread

            Application.Current.Dispatcher.Invoke(() =>

            {

                if (string.IsNullOrWhiteSpace(token))

                {

                    StatusMessage = "Social login thành công nhưng không nhận được token hệ thống.";

                    return;

                }



                _tokenStore.Save(token);

                _apiClient.Token = token;

                CloseSocialLoginWindow();



                StatusMessage = "Đăng nhập social thành công!";

                _onLoginSuccess.Invoke();

            });

        }



        private Task LoginGoogleAsync() => SocialLoginAsync(SocialProvider.Google);

        private Task LoginFacebookAsync() => SocialLoginAsync(SocialProvider.Facebook);

        private Task LoginZaloAsync() => SocialLoginAsync(SocialProvider.Zalo);



        private enum SocialProvider { Google, Facebook, Zalo }



        private async Task SocialLoginAsync(SocialProvider provider)

        {

            StatusMessage = "";

            IsBusy = true;



            try

            {

                var state = Guid.NewGuid().ToString("N");



                await EnsureHubConnectedAsync();



                // ⚠️ JoinGroupAsync phải đúng tên method trong NotificationHub (bạn đang dùng JoinGroup trong client)

                await _hubClient.JoinGroupAsync(state);



                (bool ok, string message, string? url) result = provider switch

                {

                    SocialProvider.Google => await _authApi.GetGoogleLoginUrlAsync(state),

                    SocialProvider.Facebook => await _authApi.GetFacebookLoginUrlAsync(state),

                    SocialProvider.Zalo => await _authApi.GetZaloLoginUrlAsync(state),

                    _ => (false, "Provider không hỗ trợ", null)

                };



                if (!result.ok || string.IsNullOrWhiteSpace(result.url))

                {

                    StatusMessage = result.message;

                    return;

                }



                ShowSocialLoginWindow(result.url, provider);

                StatusMessage = "Dang mo WebView de dang nhap...";

            }

            catch (Exception ex)

            {

                StatusMessage = "Lỗi social login: " + ex.Message;

            }

            finally

            {

                IsBusy = false;

            }

        }



        // -------- Register --------
        public void ResetRegisterFlow()
        {
            RegHoTen = "";
            RegTaiKhoan = "";
            RegEmail = "";
            RegSoDienThoai = "";
            RegMatKhau = "";
            RegXacNhanMatKhau = "";
            IsPolicyAccepted = false;
            StatusMessage = "";
        }

        private string _regTaiKhoan = "";

        public string RegTaiKhoan { get => _regTaiKhoan; set => SetProperty(ref _regTaiKhoan, value); }



        private string _regEmail = "";

        public string RegEmail { get => _regEmail; set => SetProperty(ref _regEmail, value); }



        private string _regMatKhau = "";

        public string RegMatKhau { get => _regMatKhau; set => SetProperty(ref _regMatKhau, value); }



        private string _regXacNhanMatKhau = "";

        public string RegXacNhanMatKhau { get => _regXacNhanMatKhau; set => SetProperty(ref _regXacNhanMatKhau, value); }



        private string _regHoTen = "";

        public string RegHoTen { get => _regHoTen; set => SetProperty(ref _regHoTen, value); }



        private string _regSoDienThoai = "";

        public string RegSoDienThoai { get => _regSoDienThoai; set => SetProperty(ref _regSoDienThoai, value); }


        private bool _isPolicyAccepted = false;

        public bool IsPolicyAccepted { get => _isPolicyAccepted; set => SetProperty(ref _isPolicyAccepted, value); }


        public AsyncRelayCommand RegisterCommand { get; }



        private async Task RegisterAsync()
        {
            StatusMessage = "";
            
            var hoten = RegHoTen?.Trim();
            var taikhoan = RegTaiKhoan?.Trim();
            var email = RegEmail?.Trim();
            var sdt = RegSoDienThoai?.Trim();
            var matkhau = RegMatKhau ?? "";
            var xnmatkhau = RegXacNhanMatKhau ?? "";

            if (string.IsNullOrEmpty(hoten) || string.IsNullOrEmpty(taikhoan) || 
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(sdt) || 
                string.IsNullOrEmpty(matkhau) || string.IsNullOrEmpty(xnmatkhau))
            {
                StatusMessage = "Vui lòng điền đầy đủ thông tin.";
                return;
            }
            if (!email.Contains("@") || !email.Contains("."))
            {
                StatusMessage = "Email không hợp lệ.";
                return;
            }
            if (matkhau != xnmatkhau)
            {
                StatusMessage = "Mật khẩu xác nhận không khớp.";
                return;
            }
            if (!IsPolicyAccepted)
            {
                StatusMessage = "Vui lòng đồng ý với chính sách dịch vụ.";
                return;
            }

            IsBusy = true;
            try
            {
                var body = new
                {

                    TaiKhoan = RegTaiKhoan?.Trim(),

                    Email = RegEmail?.Trim(),

                    MatKhau = RegMatKhau,

                    XacNhanMatKhau = RegXacNhanMatKhau,

                    HoTen = RegHoTen?.Trim(),

                    SoDienThoai = RegSoDienThoai?.Trim()

                };



                var (ok, msg) = await _authApi.RegisterAsync(body);
                StatusMessage = msg;
                // ✅ FIX: Chỉ reset form khi đăng ký thành công, không reset khi lỗi
                if (ok)
                {
                    ResetRegisterFlow();
                }
            }
            catch (Exception ex)

            {

                StatusMessage = "Lỗi đăng ký: " + ex.Message;

            }

            finally

            {

                IsBusy = false;

            }

        }



        // -------- Forgot password flow --------

        private ForgotStep _forgotStep;

        public ForgotStep ForgotStep

        {

            get => _forgotStep;

            set

            {

                if (SetProperty(ref _forgotStep, value))

                {

                    OnPropertyChanged(nameof(IsStepEmail));

                    OnPropertyChanged(nameof(IsStepOtp));

                    OnPropertyChanged(nameof(IsStepReset));

                }

            }

        }



        public bool IsStepEmail => ForgotStep == ForgotStep.EnterEmail;

        public bool IsStepOtp => ForgotStep == ForgotStep.EnterOtp;

        public bool IsStepReset => ForgotStep == ForgotStep.ResetPassword;



        private string _fpEmail = "";

        public string FpEmail { get => _fpEmail; set => SetProperty(ref _fpEmail, value); }



        private string _fpOtp = "";

        public string FpOtp { get => _fpOtp; set => SetProperty(ref _fpOtp, value); }



        private string _fpNewPassword = "";

        public string FpNewPassword { get => _fpNewPassword; set => SetProperty(ref _fpNewPassword, value); }



        private string _fpConfirmPassword = "";

        public string FpConfirmPassword { get => _fpConfirmPassword; set => SetProperty(ref _fpConfirmPassword, value); }



        public AsyncRelayCommand SendOtpCommand { get; }

        public AsyncRelayCommand VerifyOtpCommand { get; }

        public AsyncRelayCommand ResetPasswordCommand { get; }



        public void ResetForgotFlow()
        {
            ForgotStep = ForgotStep.EnterEmail;
            FpEmail = "";
            FpOtp = "";
            FpNewPassword = "";
            FpConfirmPassword = "";
            StatusMessage = "";
        }



        private async Task SendOtpAsync()

        {

            StatusMessage = "";

            var email = FpEmail?.Trim() ?? "";

            if (string.IsNullOrEmpty(email))

            {

                StatusMessage = "Vui lòng nhập địa chỉ email.";

                return;

            }

            if (!email.Contains("@") || !email.Contains("."))

            {

                StatusMessage = "Email không hợp lệ.";

                return;

            }



            IsBusy = true;

            try

            {

                var (ok, msg) = await _authApi.ForgotPasswordSendOtpAsync(email);



                StatusMessage = msg;

                if (ok) ForgotStep = ForgotStep.EnterOtp;

            }

            catch (Exception ex)

            {

                StatusMessage = "Lỗi gửi OTP: " + ex.Message;

            }

            finally

            {

                IsBusy = false;

            }

        }



        private async Task VerifyOtpAsync()

        {

            StatusMessage = "";

            var otp = FpOtp?.Trim() ?? "";

            if (string.IsNullOrEmpty(otp))

            {

                StatusMessage = "Vui lòng nhập mã OTP.";

                return;

            }

            if (otp.Length < 6)

            {

                StatusMessage = "Mã OTP không hợp lệ.";

                return;

            }



            IsBusy = true;

            try

            {

                var (ok, msg) = await _authApi.VerifyOtpAsync(FpEmail?.Trim() ?? "", otp);



                StatusMessage = msg;

                if (ok) ForgotStep = ForgotStep.ResetPassword;

            }

            catch (Exception ex)

            {

                StatusMessage = "Lỗi xác nhận OTP: " + ex.Message;

            }

            finally

            {

                IsBusy = false;

            }

        }



        private async Task ResetPasswordAsync()

        {

            StatusMessage = "";

            var email = FpEmail?.Trim() ?? "";

            var newPass = FpNewPassword ?? "";

            var confirmPass = FpConfirmPassword ?? "";



            if (string.IsNullOrEmpty(newPass))

            {

                StatusMessage = "Vui lòng nhập mật khẩu mới.";

                return;

            }

            if (string.IsNullOrEmpty(confirmPass))

            {

                StatusMessage = "Vui lòng xác nhận mật khẩu.";

                return;

            }

            if (newPass != confirmPass)

            {

                StatusMessage = "Mật khẩu xác nhận không khớp.";

                return;

            }



            IsBusy = true;

            try

            {

                var (ok, msg) = await _authApi.ResetPasswordAsync(email, newPass, confirmPass);



                StatusMessage = msg;
                if (ok)
                {
                    ResetForgotFlow();
                }

            }

            catch (Exception ex)

            {

                StatusMessage = "Lỗi đổi mật khẩu: " + ex.Message;

            }

            finally

            {

                IsBusy = false;

            }

        }

    }

}
