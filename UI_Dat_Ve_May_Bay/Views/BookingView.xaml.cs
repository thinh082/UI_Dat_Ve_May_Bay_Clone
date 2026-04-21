using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using UI_Dat_Ve_May_Bay.ViewModels;

namespace UI_Dat_Ve_May_Bay.Views
{
    public partial class BookingView : UserControl
    {
        private DispatcherTimer? _autoRefreshTimer;
        private int _autoRefreshTick;
        private INotifyPropertyChanged? _vmNotify;
        private bool _webHooked;

        public BookingView()
        {
            InitializeComponent();
            DataContextChanged += BookingView_DataContextChanged;
        }

        private void BookingView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vmNotify != null)
                _vmNotify.PropertyChanged -= Vm_PropertyChanged;

            _vmNotify = e.NewValue as INotifyPropertyChanged;
            if (_vmNotify != null)
                _vmNotify.PropertyChanged += Vm_PropertyChanged;

            _ = EnsurePaymentWebReadyAsync();
            _ = SyncPaymentWebFromVmAsync();
        }

        private async void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BookingViewModel.PaymentUrl) || e.PropertyName == nameof(BookingViewModel.IsWaitingPayment))
            {
                await Dispatcher.InvokeAsync(async () => await SyncPaymentWebFromVmAsync());
            }
        }

        private async System.Threading.Tasks.Task EnsurePaymentWebReadyAsync()
        {
            try
            {
                if (PaymentWeb.CoreWebView2 == null)
                {
                    await PaymentWeb.EnsureCoreWebView2Async();
                }

                if (!_webHooked && PaymentWeb.CoreWebView2 != null)
                {
                    PaymentWeb.NavigationCompleted += PaymentWeb_NavigationCompleted;
                    _webHooked = true;
                }
            }
            catch
            {
                // Nếu máy thiếu WebView2 Runtime, vẫn cho user dùng nút mở browser fallback.
            }
        }

        private async System.Threading.Tasks.Task SyncPaymentWebFromVmAsync()
        {
            if (DataContext is not BookingViewModel vm) return;
            if (!vm.IsWaitingPayment) return;
            if (string.IsNullOrWhiteSpace(vm.PaymentUrl)) return;

            await EnsurePaymentWebReadyAsync();

            try
            {
                var current = PaymentWeb.Source?.ToString() ?? PaymentWeb.CoreWebView2?.Source ?? "";
                if (!string.Equals(current, vm.PaymentUrl, StringComparison.OrdinalIgnoreCase))
                {
                    PaymentWeb.Source = new Uri(vm.PaymentUrl);
                }
            }
            catch
            {
                // ignore, fallback buttons still work
            }
        }

        private void PaymentWeb_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (DataContext is not BookingViewModel vm) return;

            try
            {
                var url = PaymentWeb.Source?.ToString() ?? PaymentWeb.CoreWebView2?.Source ?? "";
                if (string.IsNullOrWhiteSpace(url)) return;

                var u = url.ToLowerInvariant();
                if (u.Contains("callback") || u.Contains("return") || u.Contains("capture") || u.Contains("vnpay") || u.Contains("paypal"))
                {
                    if (vm.CheckPaymentStatusCommand.CanExecute(null))
                        vm.CheckPaymentStatusCommand.Execute(null);
                }
            }
            catch { }
        }

        private void ClosePaymentOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BookingViewModel vm)
            {
                vm.IsWaitingPayment = false;
            }
        }

        private void BookingView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_autoRefreshTimer != null) return;

            _autoRefreshTick = 0;
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
            _autoRefreshTimer.Start();

            _ = EnsurePaymentWebReadyAsync();
            _ = SyncPaymentWebFromVmAsync();
        }

        private async void BookingView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_vmNotify != null)
            {
                _vmNotify.PropertyChanged -= Vm_PropertyChanged;
                _vmNotify = null;
            }

            if (_autoRefreshTimer != null)
            {
                _autoRefreshTimer.Stop();
                _autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
                _autoRefreshTimer = null;
            }

            if (_webHooked)
            {
                PaymentWeb.NavigationCompleted -= PaymentWeb_NavigationCompleted;
                _webHooked = false;
            }

            if (DataContext is BookingViewModel vm)
            {
                await vm.ReleaseAllHeldSeatsAsync();
            }
        }

        private void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is not BookingViewModel vm) return;
            if (vm.IsBusy) return;
            if (vm.IsWaitingPayment) return;

            if (vm.AutoRefreshSeatsCommand.CanExecute(null))
                vm.AutoRefreshSeatsCommand.Execute(null);
            _autoRefreshTick++;
            if (_autoRefreshTick % 4 == 0)
            {
                vm.LoadVouchersCommand.Execute(null);
            }
        }

        private async void Seat_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BookingViewModel vm) return;
            if (sender is not ToggleButton tb) return;
            if (tb.DataContext is not BookingViewModel.SeatVm seat) return;

            await vm.OnSeatCheckedAsync(seat);
        }

        private async void Seat_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DataContext is not BookingViewModel vm) return;
            if (sender is not ToggleButton tb) return;
            if (tb.DataContext is not BookingViewModel.SeatVm seat) return;

            await vm.OnSeatUncheckedAsync(seat);
        }
    }
}
