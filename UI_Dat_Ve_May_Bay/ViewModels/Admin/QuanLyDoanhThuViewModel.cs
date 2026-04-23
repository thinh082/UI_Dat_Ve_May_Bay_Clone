using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Extensions;
using SkiaSharp;
using System;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Api.Admin;
using UI_Dat_Ve_May_Bay.Core;

namespace UI_Dat_Ve_May_Bay.ViewModels.Admin
{
    public class QuanLyDoanhThuViewModel : ObservableObject, IAdminRefreshable
    {
        private readonly QuanLyDoanhThuApi _api;

        public QuanLyDoanhThuViewModel(QuanLyDoanhThuApi api)
        {
            _api = api;

            Ngay = DateTime.Today.ToString("dd-MM-yyyy");
            Thang = DateTime.Today.ToString("MM-yyyy");
            Nam = DateTime.Today.Year.ToString();

            RevenueXAxes = new[]
            {
                new Axis
                {
                    Labels = new[] { "Ngày", "Tháng", "Năm" },
                    LabelsPaint = new SolidColorPaint(new SKColor(15, 76, 129)),
                    TextSize = 13
                }
            };

            RevenueYAxes = new[]
            {
                new Axis
                {
                    Labeler = value => $"{value / 1_000_000d:0.#}M",
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(new SKColor(72, 101, 129)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(223, 232, 242)) { StrokeThickness = 1 }
                }
            };

            TaiDoanhThuNgayCommand = new AsyncRelayCommand(TaiDoanhThuNgayAsync, () => !IsBusy);
            TaiDoanhThuThangCommand = new AsyncRelayCommand(TaiDoanhThuThangAsync, () => !IsBusy);
            TaiDoanhThuNamCommand = new AsyncRelayCommand(TaiDoanhThuNamAsync, () => !IsBusy);
            TaiTatCaCommand = new AsyncRelayCommand(TaiTatCaAsync, () => !IsBusy);

            UpdateChart();
            UpdateAirlineRevenueChart();
            _ = TaiTatCaAsync();
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

        private string _ngay = string.Empty;
        public string Ngay
        {
            get => _ngay;
            set => SetProperty(ref _ngay, value);
        }

        private string _thang = string.Empty;
        public string Thang
        {
            get => _thang;
            set => SetProperty(ref _thang, value);
        }

        private string _nam = string.Empty;
        public string Nam
        {
            get => _nam;
            set => SetProperty(ref _nam, value);
        }

        private decimal _doanhThuNgay;
        public decimal DoanhThuNgay
        {
            get => _doanhThuNgay;
            set
            {
                if (SetProperty(ref _doanhThuNgay, value))
                    OnRevenueChanged();
            }
        }

        public string DoanhThuNgayText => $"{DoanhThuNgay:N0} VND";

        private decimal _doanhThuThang;
        public decimal DoanhThuThang
        {
            get => _doanhThuThang;
            set
            {
                if (SetProperty(ref _doanhThuThang, value))
                    OnRevenueChanged();
            }
        }

        public string DoanhThuThangText => $"{DoanhThuThang:N0} VND";

        private decimal _doanhThuNam;
        public decimal DoanhThuNam
        {
            get => _doanhThuNam;
            set
            {
                if (SetProperty(ref _doanhThuNam, value))
                    OnRevenueChanged();
            }
        }

        public string DoanhThuNamText => $"{DoanhThuNam:N0} VND";
        public string TongHopDoanhThuText => $"{(DoanhThuNgay + DoanhThuThang + DoanhThuNam):N0} VND";

        public string MocNoiBatText
        {
            get
            {
                if (DoanhThuNam >= DoanhThuThang && DoanhThuNam >= DoanhThuNgay) return "Năm đang cao nhất";
                if (DoanhThuThang >= DoanhThuNgay) return "Tháng đang cao nhất";
                return "Ngày đang cao nhất";
            }
        }

        public ISeries[] RevenueSeries { get; private set; } = Array.Empty<ISeries>();
        public Axis[] RevenueXAxes { get; }
        public Axis[] RevenueYAxes { get; }
        
        // Biểu đồ tròn - Doanh thu theo hãng bay
        public ISeries[] AirlineRevenuePieSeries { get; private set; } = Array.Empty<ISeries>();

        public AsyncRelayCommand TaiDoanhThuNgayCommand { get; }
        public AsyncRelayCommand TaiDoanhThuThangCommand { get; }
        public AsyncRelayCommand TaiDoanhThuNamCommand { get; }
        public AsyncRelayCommand TaiTatCaCommand { get; }

        private async Task TaiTatCaAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var ngayTask = _api.DoanhThuTheoNgayAsync(Ngay);
                var thangTask = _api.DoanhThuTheoThangAsync(Thang);
                var namTask = _api.DoanhThuTheoNamAsync(Nam);

                await Task.WhenAll(ngayTask, thangTask, namTask);

                DoanhThuNgay = (await ngayTask).DoanhThu;
                DoanhThuThang = (await thangTask).DoanhThu;
                DoanhThuNam = (await namTask).DoanhThu;
                StatusMessage = "Đã tải doanh thu ngày, tháng và năm.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải doanh thu: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task TaiDoanhThuNgayAsync()
        {
            await TaiMotMocAsync(async () =>
            {
                var result = await _api.DoanhThuTheoNgayAsync(Ngay);
                DoanhThuNgay = result.DoanhThu;
            }, "ngày");
        }

        private async Task TaiDoanhThuThangAsync()
        {
            await TaiMotMocAsync(async () =>
            {
                var result = await _api.DoanhThuTheoThangAsync(Thang);
                DoanhThuThang = result.DoanhThu;
            }, "tháng");
        }

        private async Task TaiDoanhThuNamAsync()
        {
            await TaiMotMocAsync(async () =>
            {
                var result = await _api.DoanhThuTheoNamAsync(Nam);
                DoanhThuNam = result.DoanhThu;
            }, "năm");
        }

        private async Task TaiMotMocAsync(Func<Task> action, string type)
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                await action();
                StatusMessage = $"Đã cập nhật doanh thu theo {type}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải doanh thu theo {type}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnRevenueChanged()
        {
            OnPropertyChanged(nameof(DoanhThuNgayText));
            OnPropertyChanged(nameof(DoanhThuThangText));
            OnPropertyChanged(nameof(DoanhThuNamText));
            OnPropertyChanged(nameof(TongHopDoanhThuText));
            OnPropertyChanged(nameof(MocNoiBatText));
            UpdateChart();
        }

        private void UpdateChart()
        {
            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Name = "Doanh thu",
                    Values = new[] { DoanhThuNgay, DoanhThuThang, DoanhThuNam },
                    Fill = new SolidColorPaint(new SKColor(15, 108, 189)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(16, 42, 67)),
                    DataLabelsPosition = DataLabelsPosition.Top,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:N0}"
                }
            };

            OnPropertyChanged(nameof(RevenueSeries));
        }

        private void UpdateAirlineRevenueChart()
        {
            // Mock data - Doanh thu theo hãng bay (cần API thực tế)
            var airlines = new[]
            {
                new { Name = "Vietnam Airlines", Revenue = 12500000m, Color = new SKColor(15, 108, 189) },
                new { Name = "Bamboo Airways", Revenue = 8300000m, Color = new SKColor(21, 128, 61) },
                new { Name = "Vietjet Air", Revenue = 9800000m, Color = new SKColor(220, 38, 38) },
                new { Name = "Vietravel Airlines", Revenue = 3200000m, Color = new SKColor(124, 58, 237) },
                new { Name = "Pacific Airlines", Revenue = 2100000m, Color = new SKColor(234, 179, 8) }
            };

            var totalRevenue = 0m;
            foreach (var airline in airlines)
                totalRevenue += airline.Revenue;

            var series = new ISeries[airlines.Length];
            for (int i = 0; i < airlines.Length; i++)
            {
                var airline = airlines[i];
                series[i] = new PieSeries<decimal>
                {
                    Name = airline.Name,
                    Values = new[] { airline.Revenue },
                    Fill = new SolidColorPaint(airline.Color),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 13,
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{(point.Coordinate.PrimaryValue / (double)totalRevenue * 100):0.#}%"
                };
            }

            AirlineRevenuePieSeries = series;
            OnPropertyChanged(nameof(AirlineRevenuePieSeries));
        }

        public Task RefreshAsync() => TaiTatCaAsync();

        private void RaiseCommandStates()
        {
            TaiDoanhThuNgayCommand.RaiseCanExecuteChanged();
            TaiDoanhThuThangCommand.RaiseCanExecuteChanged();
            TaiDoanhThuNamCommand.RaiseCanExecuteChanged();
            TaiTatCaCommand.RaiseCanExecuteChanged();
        }
    }
}


