using System.Windows.Controls;

using UI_Dat_Ve_May_Bay.ViewModels;



namespace UI_Dat_Ve_May_Bay.Views

{

    public partial class AuthView : UserControl

    {

        public AuthView()

        {

            InitializeComponent();

        }



        private AuthViewModel? VM => DataContext as AuthViewModel;

        private void GoToRegister_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MainTabControl != null) MainTabControl.SelectedIndex = 1;
            if (VM != null) VM.ResetRegisterFlow();
            if (RegPasswordBox != null) RegPasswordBox.Password = "";
            if (RegConfirmPasswordBox != null) RegConfirmPasswordBox.Password = "";
        }

        private void GoToForgot_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MainTabControl != null) MainTabControl.SelectedIndex = 2;
            if (VM != null) VM.ResetForgotFlow();
            if (FpNewPasswordBox != null) FpNewPasswordBox.Password = "";
            if (FpConfirmPasswordBox != null) FpConfirmPasswordBox.Password = "";
        }

        private void GoToLogin_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MainTabControl != null) MainTabControl.SelectedIndex = 0;
            if (VM != null) VM.StatusMessage = string.Empty;
            if (LoginPasswordBox != null) LoginPasswordBox.Password = "";
        }

        private bool _isLoginPasswordVisible = false;
        private void ToggleLoginPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isLoginPasswordVisible = !_isLoginPasswordVisible;
            if (_isLoginPasswordVisible)
            {
                if (LoginVisiblePasswordBox != null) LoginVisiblePasswordBox.Visibility = System.Windows.Visibility.Visible;
                if (LoginPasswordBox != null) LoginPasswordBox.Visibility = System.Windows.Visibility.Collapsed;
                if (LoginEyeIcon != null) 
                {
                    LoginEyeIcon.Text = "🙈";
                    LoginEyeIcon.ToolTip = "Ẩn mật khẩu";
                }
            }
            else
            {
                if (LoginVisiblePasswordBox != null) LoginVisiblePasswordBox.Visibility = System.Windows.Visibility.Collapsed;
                if (LoginPasswordBox != null) 
                {
                    LoginPasswordBox.Visibility = System.Windows.Visibility.Visible;
                    if (VM != null) LoginPasswordBox.Password = VM.LoginMatKhau;
                }
                if (LoginEyeIcon != null)
                {
                    LoginEyeIcon.Text = "👁";
                    LoginEyeIcon.ToolTip = "Hiển thị mật khẩu";
                }
            }
        }



        private void LoginPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)

        {

            if (VM != null) VM.LoginMatKhau = ((PasswordBox)sender).Password;

        }



        private void RegPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)

        {

            if (VM != null) VM.RegMatKhau = ((PasswordBox)sender).Password;

        }



        private void RegConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)

        {

            if (VM != null) VM.RegXacNhanMatKhau = ((PasswordBox)sender).Password;

        }



        private void FpNewPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)

        {

            if (VM != null) VM.FpNewPassword = ((PasswordBox)sender).Password;

        }



        private void FpConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)

        {

            if (VM != null) VM.FpConfirmPassword = ((PasswordBox)sender).Password;

        }

        private void ShowPolicy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var policyWindow = new PolicyWindow();
            var result = policyWindow.ShowDialog();

            if (result == true)
            {
                if (PolicyCheckBox != null) PolicyCheckBox.IsChecked = true;
            }
        }

    }

}

