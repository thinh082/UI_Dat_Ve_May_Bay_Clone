using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;

namespace UI_Dat_Ve_May_Bay.Views.Components
{
    public partial class SocialLoginWebViewWindow : Window
    {
        private readonly string _url;

        public SocialLoginWebViewWindow(string url, string providerName)
        {
            InitializeComponent();
            _url = url;
            Title = $"Sign in with {providerName}";
            HeaderText.Text = $"Complete {providerName} sign in";
            Loaded += SocialLoginWebViewWindow_Loaded;
        }

        private async void SocialLoginWebViewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await Browser.EnsureCoreWebView2Async();
                if (Browser.CoreWebView2 is not null)
                {
                    Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                }

                Browser.NavigationStarting += Browser_NavigationStarting;
                Browser.NavigationCompleted += Browser_NavigationCompleted;
                Browser.Source = new Uri(_url);
                UpdateBackButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Cannot open embedded browser: {ex.Message}",
                    "WebView2 error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            UpdateBackButtonState();
        }

        private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            UpdateBackButtonState();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack)
            {
                Browser.GoBack();
            }

            UpdateBackButtonState();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            Browser.Reload();
        }

        private void UpdateBackButtonState()
        {
            BackButton.IsEnabled = Browser.CanGoBack;
        }

        protected override void OnClosed(EventArgs e)
        {
            Browser.NavigationStarting -= Browser_NavigationStarting;
            Browser.NavigationCompleted -= Browser_NavigationCompleted;
            base.OnClosed(e);
        }
    }
}
