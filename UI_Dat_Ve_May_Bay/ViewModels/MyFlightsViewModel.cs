using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Core;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class MyFlightsViewModel : ObservableObject
    {
        private readonly DichVuApi _dichVuApi;

        public ObservableCollection<MyFlightItemVm> Flights { get; } = new();

        private MyFlightItemVm? _selectedFlight;
        public MyFlightItemVm? SelectedFlight
        {
            get => _selectedFlight;
            set
            {
                if (SetProperty(ref _selectedFlight, value))
                {
                    CancelReason = string.Empty;
                    BankName = string.Empty;
                    BankAccountNumber = string.Empty;
                    OnPropertyChanged(nameof(HasSelection));
                    RefreshCommand.RaiseCanExecuteChanged();
                    LoadDetailCommand.RaiseCanExecuteChanged();
                    CancelTicketCommand.RaiseCanExecuteChanged();
                    _ = LoadDetailAsync();
                }
            }
        }

        private MyFlightDetailVm? _selectedDetail;
        public MyFlightDetailVm? SelectedDetail
        {
            get => _selectedDetail;
            set => SetProperty(ref _selectedDetail, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    LoadDetailCommand.RaiseCanExecuteChanged();
                    CancelTicketCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isLoadingDetail;
        public bool IsLoadingDetail
        {
            get => _isLoadingDetail;
            set => SetProperty(ref _isLoadingDetail, value);
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                    OnPropertyChanged(nameof(HasStatus));
            }
        }

        private string _error = "";
        public string Error
        {
            get => _error;
            set
            {
                if (SetProperty(ref _error, value))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        private string _cancelReason = "";
        public string CancelReason
        {
            get => _cancelReason;
            set => SetProperty(ref _cancelReason, value);
        }

        private string _bankName = "";
        public string BankName
        {
            get => _bankName;
            set => SetProperty(ref _bankName, value);
        }

        private string _bankAccountNumber = "";
        public string BankAccountNumber
        {
            get => _bankAccountNumber;
            set => SetProperty(ref _bankAccountNumber, value);
        }

        public bool HasStatus => !string.IsNullOrWhiteSpace(Status);
        public bool HasError => !string.IsNullOrWhiteSpace(Error);
        public bool HasFlights => Flights.Count > 0;
        public bool HasSelection => SelectedFlight != null;

        public AsyncRelayCommand RefreshCommand { get; }
        public AsyncRelayCommand LoadDetailCommand { get; }
        public AsyncRelayCommand CancelTicketCommand { get; }

        public MyFlightsViewModel(DichVuApi dichVuApi, bool autoLoad = true)
        {
            _dichVuApi = dichVuApi;

            RefreshCommand = new AsyncRelayCommand(LoadFlightsAsync, () => !IsBusy);
            LoadDetailCommand = new AsyncRelayCommand(LoadDetailAsync, () => !IsBusy && SelectedFlight != null);
            CancelTicketCommand = new AsyncRelayCommand(CancelTicketAsync, CanCancelTicket);

            if (autoLoad)
                _ = LoadFlightsAsync();
        }

        public async Task LoadFlightsAsync()
        {
            try
            {
                IsBusy = true;
                Error = "";
                Status = "Đang tải chuyến bay của bạn...";

                var (items, message) = await _dichVuApi.LayDanhSachVeMayBayAsync();
                var selectedId = SelectedFlight?.Id ?? 0;

                Flights.Clear();
                foreach (var item in items)
                {
                    Flights.Add(new MyFlightItemVm
                    {
                        Id = item.Id,
                        IdLichBay = item.IdLichBay,
                        NgayDat = item.NgayDat,
                        DiemDi = item.DiemDi,
                        DiemDen = item.DiemDen,
                        ThoiGianBatDau = item.ThoiGianBatDau,
                        ThoiGianKetThuc = item.ThoiGianKetThuc,
                        TrangThaiRaw = item.TrangThaiRaw
                    });
                }

                OnPropertyChanged(nameof(HasFlights));

                // ✅ FIX: Clear SelectedDetail trước khi set SelectedFlight
                SelectedDetail = null;
                
                if (Flights.Count > 0)
                {
                    SelectedFlight = Flights.FirstOrDefault(x => x.Id == selectedId) ?? Flights[0];
                }
                else
                {
                    SelectedFlight = null;
                }

                Status = Flights.Count == 0
                    ? "Bạn chưa có chuyến bay nào."
                    : $"Đã tải {Flights.Count} chuyến bay.";

                if (!string.IsNullOrWhiteSpace(message) && Flights.Count == 0)
                    Status = message;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Status = "Không tải được danh sách chuyến bay.";
            }
            finally
            {
                IsBusy = false;
                RefreshCommand.RaiseCanExecuteChanged();
                LoadDetailCommand.RaiseCanExecuteChanged();
                CancelTicketCommand.RaiseCanExecuteChanged();
            }
        }

        public async Task LoadDetailAsync()
        {
            if (SelectedFlight == null)
            {
                SelectedDetail = null;
                return;
            }

            try
            {
                IsLoadingDetail = true;
                Error = "";

                var (detail, _) = await _dichVuApi.LayChiTietVeMayBayAsync(SelectedFlight.Id);

                if (detail == null)
                {
                    SelectedDetail = new MyFlightDetailVm
                    {
                        Id = SelectedFlight.Id,
                        DiemDi = SelectedFlight.DiemDi,
                        DiemDen = SelectedFlight.DiemDen,
                        ThoiGianBatDau = SelectedFlight.ThoiGianBatDau,
                        ThoiGianKetThuc = SelectedFlight.ThoiGianKetThuc,
                        NgayDat = SelectedFlight.NgayDat
                    };
                    return;
                }

                SelectedDetail = new MyFlightDetailVm
                {
                    Id = detail.Id,
                    NgayDat = detail.NgayDat,
                    DiemDi = detail.DiemDi,
                    DiemDen = detail.DiemDen,
                    MaSanBayDi = detail.MaSanBayDi,
                    MaSanBayDen = detail.MaSanBayDen,
                    ThoiGianBatDau = detail.ThoiGianBatDau,
                    ThoiGianKetThuc = detail.ThoiGianKetThuc
                };
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsLoadingDetail = false;
            }
        }

        public async Task CancelTicketAsync()
        {
            if (SelectedFlight == null) return;

            if (MessageBox.Show(
                    $"Bạn có chắc muốn hủy vé #{SelectedFlight.Id}?",
                    "Xác nhận hủy vé",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                IsBusy = true;
                Error = "";
                Status = "Đang gửi yêu cầu hủy vé...";

                var reason = string.IsNullOrWhiteSpace(CancelReason)
                    ? "Hủy từ màn Chuyến bay của bạn"
                    : CancelReason.Trim();

                var message = await _dichVuApi.HuyVeMayBayAsync(SelectedFlight.Id, reason);
                await LoadFlightsAsync();
                Status = message;
                CancelReason = string.Empty;
                BankName = string.Empty;
                BankAccountNumber = string.Empty;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Status = "Hủy vé thất bại.";
            }
            finally
            {
                IsBusy = false;
                CancelTicketCommand.RaiseCanExecuteChanged();
            }
        }

        private bool CanCancelTicket()
        {
            if (IsBusy || SelectedFlight == null) return false;

            var status = (SelectedFlight.TrangThaiRaw ?? "").ToLowerInvariant();
            if (status.Contains("hủy") || status.Contains("huy"))
                return false;

            return true;
        }

        public class MyFlightItemVm : ObservableObject
        {
            public long Id { get; set; }
            public long IdLichBay { get; set; }
            public DateTime? NgayDat { get; set; }
            public string DiemDi { get; set; } = "";
            public string DiemDen { get; set; } = "";
            public DateTime? ThoiGianBatDau { get; set; }
            public DateTime? ThoiGianKetThuc { get; set; }
            public string? TrangThaiRaw { get; set; }

            public string RouteText => $"{DiemDi} → {DiemDen}";
            public string BookedAtText => NgayDat.HasValue ? NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "--";
            public string StartText => ThoiGianBatDau.HasValue ? ThoiGianBatDau.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "--";
            public string EndText => ThoiGianKetThuc.HasValue ? ThoiGianKetThuc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "--";
            public string StatusText => string.IsNullOrWhiteSpace(TrangThaiRaw) ? "--" : TrangThaiRaw!;
        }

        public class MyFlightDetailVm : ObservableObject
        {
            public long Id { get; set; }
            public DateTime? NgayDat { get; set; }
            public string DiemDi { get; set; } = "";
            public string DiemDen { get; set; } = "";
            public string MaSanBayDi { get; set; } = "";
            public string MaSanBayDen { get; set; } = "";
            public DateTime? ThoiGianBatDau { get; set; }
            public DateTime? ThoiGianKetThuc { get; set; }

            public string RouteText => $"{DiemDi} → {DiemDen}";
            public string RouteCodeText => $"{(string.IsNullOrWhiteSpace(MaSanBayDi) ? "?" : MaSanBayDi)} → {(string.IsNullOrWhiteSpace(MaSanBayDen) ? "?" : MaSanBayDen)}";
            public string BookedAtText => NgayDat.HasValue ? NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "--";
            public string StartText => ThoiGianBatDau.HasValue ? ThoiGianBatDau.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "--";
            public string EndText => ThoiGianKetThuc.HasValue ? ThoiGianKetThuc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "--";
        }
    }
}
