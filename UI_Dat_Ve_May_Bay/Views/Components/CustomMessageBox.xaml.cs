using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI_Dat_Ve_May_Bay.Views.Components
{
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxType
        {
            Info,
            Success,
            Warning,
            Error,
            Confirmation
        }

        public bool Result { get; private set; }

        public CustomMessageBox(string message, string title, MessageBoxType type)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
            SetAppearanceBaseOnType(type);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ Animation: Fade in + Scale
            var fadeIn = new DoubleAnimation(0, 1, System.TimeSpan.FromMilliseconds(300));
            var scaleIn = new DoubleAnimation(0.95, 1, System.TimeSpan.FromMilliseconds(300));
            
            this.BeginAnimation(OpacityProperty, fadeIn);
            
            var scaleTransform = new ScaleTransform(0.95, 0.95);
            this.RenderTransform = scaleTransform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
        }

        private void SetAppearanceBaseOnType(MessageBoxType type)
        {
            switch (type)
            {
                case MessageBoxType.Info:
                    IconPath.Data = Geometry.Parse("M11,9H13V7H11M12,20C7.59,20 4,16.41 4,12C4,7.59 7.59,4 12,4C16.41,4 20,7.59 20,12C20,16.41 16.41,20 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,17H13V11H11V17Z");
                    IconPath.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#0EA5E9")!;
                    IconBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#E0F2FE")!;
                    break;
                case MessageBoxType.Success:
                    IconPath.Data = Geometry.Parse("M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M10,17L5,12L6.41,10.59L10,14.17L17.59,6.58L19,8L10,17Z");
                    IconPath.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#10B981")!;
                    IconBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#D1FAE5")!;
                    break;
                case MessageBoxType.Warning:
                    IconPath.Data = Geometry.Parse("M12,2L1,21H23M12,6L19.53,19H4.47M11,10V14H13V10M11,16V18H13V16");
                    IconPath.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#F59E0B")!;
                    IconBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FEF3C7")!;
                    break;
                case MessageBoxType.Error:
                    IconPath.Data = Geometry.Parse("M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z");
                    IconPath.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#EF4444")!;
                    IconBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FEE2E2")!;
                    BtnOk.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#EF4444")!;
                    break;
                case MessageBoxType.Confirmation:
                    IconPath.Data = Geometry.Parse("M15.5,12C15.5,10.07 14.54,8.37 13.06,7.44C13.27,6.86 13.5,6 13.5,6C16.85,7.21 19.38,10.19 19.9,13.9C20.31,16.89 19,19.64 16.71,21.26C14.41,22.88 11.45,23.08 9,21.75C6.56,20.43 5,17.9 5,15.15C5,14.65 5.07,14.15 5.18,13.68C6.67,14.07 8.24,14 9.68,13.5C8.95,14.93 9.42,16.71 10.81,17.55C12.19,18.39 14.03,18 14.92,16.66C15.82,15.31 15.5,13.43 15.5,12M12,2A3,3 0 0,0 9,5C9,5.77 9.29,6.47 9.77,7C9.3,7.5 9,8.2 9,9V9.5A2.5,2.5 0 0,0 11.5,12C12.88,12 14,10.88 14,9.5V9C14,8.2 13.7,7.5 13.23,7C13.71,6.47 14,5.77 14,5A3,3 0 0,0 12,2M12,4A1,1 0 0,1 13,5A1,1 0 0,1 12,6A1,1 0 0,1 11,5A1,1 0 0,1 12,4M12,8A1,1 0 0,1 13,9V9.5A0.5,0.5 0 0,1 12.5,10A0.5,0.5 0 0,1 12,9.5V9A1,1 0 0,1 12,8Z");
                    IconPath.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#0EA5E9")!;
                    IconBackground.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#E0F2FE")!;
                    
                    BtnOk.Visibility = Visibility.Collapsed;
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }
    }
}
