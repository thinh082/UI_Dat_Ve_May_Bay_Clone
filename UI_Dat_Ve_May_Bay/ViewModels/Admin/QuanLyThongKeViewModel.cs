using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Extensions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using UI_Dat_Ve_May_Bay.Api.Admin;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Models.Admin;

namespace UI_Dat_Ve_May_Bay.ViewModels.Admin
{
    public class QuanLyThongKeViewModel : ObservableObject, IAdminRefreshable
    {
        private readonly QuanLyThongKeApi _api;

        public ObservableCollection<QuanLyThongKeDoanhThuNgayDto> DoanhThuTheoNgay { get; } = new();
        public ObservableCollection<QuanLyThongKeVeNgayDto> VeTheoNgay { get; } = new();

        public QuanLyThongKeViewModel(QuanLyThongKeApi api)
        {
            _api = api;

            TuNgay = DateTime.Today.AddDays(-14);
            DenNgay = DateTime.Today;

            TaiThongKeCommand = new AsyncRelayCommand(TaiThongKeAsync, () => !IsBusy);
            DatLaiKhoangNgayCommand = new RelayCommand(DatLaiKhoangNgay);

            RevenueXAxes = Array.Empty<Axis>();
            RevenueYAxes = BuildRevenueYAxes();
            TicketXAxes = Array.Empty<Axis>();
            TicketYAxes = BuildTicketYAxes();
            RevenueSeries = Array.Empty<ISeries>();
            TicketSeries = Array.Empty<ISeries>();
            TicketStatusPieSeries = Array.Empty<ISeries>();
            TopRoutesSeries = Array.Empty<ISeries>();
            TopRoutesXAxes = Array.Empty<Axis>();
            TopRoutesYAxes = BuildTopRoutesYAxes();

            _ = TaiThongKeAsync();
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                    TaiThongKeCommand.RaiseCanExecuteChanged();
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

        private DateTime? _tuNgay;
        public DateTime? TuNgay
        {
            get => _tuNgay;
            set => SetProperty(ref _tuNgay, value);
        }

        private DateTime? _denNgay;
        public DateTime? DenNgay
        {
            get => _denNgay;
            set => SetProperty(ref _denNgay, value);
        }

        private decimal _doanhThuHomNay;
        public decimal DoanhThuHomNay
        {
            get => _doanhThuHomNay;
            set
            {
                if (SetProperty(ref _doanhThuHomNay, value))
                    OnPropertyChanged(nameof(DoanhThuHomNayText));
            }
        }

        public string DoanhThuHomNayText => $"{DoanhThuHomNay:N0} VND";

        private int _tongVeDaBan;
        public int TongVeDaBan
        {
            get => _tongVeDaBan;
            set => SetProperty(ref _tongVeDaBan, value);
        }

        private int _nguoiDungHoatDong;
        public int NguoiDungHoatDong
        {
            get => _nguoiDungHoatDong;
            set => SetProperty(ref _nguoiDungHoatDong, value);
        }

        private double _tiLeHuy;
        public double TiLeHuy
        {
            get => _tiLeHuy;
            set
            {
                if (SetProperty(ref _tiLeHuy, value))
                    OnPropertyChanged(nameof(TiLeHuyText));
            }
        }

        public string TiLeHuyText => $"{TiLeHuy:0.##}%";
        public string TongDoanhThuRangeText => $"{DoanhThuTheoNgay.Sum(x => x.DoanhThu):N0} VND";
        public int TongVeRange => VeTheoNgay.Sum(x => x.TongVe);
        public string TongVeRangeText => $"{TongVeRange} vé";
        public string KhoangNgayText => $"{(TuNgay?.ToString("dd/MM/yyyy") ?? "--")} - {(DenNgay?.ToString("dd/MM/yyyy") ?? "--")}";
        public string RevenueTrendText => BuildRevenueTrendText();
        public string TicketTrendText => BuildTicketTrendText();
        public Brush RevenueTrendBrush => BuildTrendBrush(GetRevenueTrendDelta());
        public Brush TicketTrendBrush => BuildTrendBrush(GetTicketTrendDelta());

        public string NgayDoanhThuCaoNhatText
        {
            get
            {
                var item = DoanhThuTheoNgay.OrderByDescending(x => x.DoanhThu).FirstOrDefault();
                return item == null ? "--" : $"{item.Ngay:dd/MM/yyyy} / {item.DoanhThu:N0} VND";
            }
        }

        public string NgayVeCaoNhatText
        {
            get
            {
                var item = VeTheoNgay.OrderByDescending(x => x.TongVe).FirstOrDefault();
                return item == null ? "--" : $"{item.Ngay:dd/MM/yyyy} / {item.TongVe} vé";
            }
        }

        public ISeries[] RevenueSeries { get; private set; }
        public Axis[] RevenueXAxes { get; private set; }
        public Axis[] RevenueYAxes { get; }
        public ISeries[] TicketSeries { get; private set; }
        public Axis[] TicketXAxes { get; private set; }
        public Axis[] TicketYAxes { get; }
        
        // Biểu đồ tròn - Tỷ lệ vé theo trạng thái
        public ISeries[] TicketStatusPieSeries { get; private set; }
        
        // Biểu đồ cột - Top tuyến đường
        public ISeries[] TopRoutesSeries { get; private set; }
        public Axis[] TopRoutesXAxes { get; private set; }
        public Axis[] TopRoutesYAxes { get; }

        public AsyncRelayCommand TaiThongKeCommand { get; }
        public RelayCommand DatLaiKhoangNgayCommand { get; }

        private async Task TaiThongKeAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var dashboardTask = _api.GetDashboardKpiAsync();
                var revenueTask = _api.DoanhThuTheoNgayRangeAsync(TuNgay, DenNgay);
                var ticketTask = _api.VeTheoNgayRangeAsync(TuNgay, DenNgay);

                await Task.WhenAll(dashboardTask, revenueTask, ticketTask);

                var dashboard = await dashboardTask;
                var revenues = await revenueTask;
                var tickets = await ticketTask;

                DoanhThuHomNay = dashboard?.DoanhThuHomNay ?? 0;
                TongVeDaBan = dashboard?.TongVeDaBan ?? 0;
                NguoiDungHoatDong = dashboard?.NguoiDungHoatDong ?? 0;
                TiLeHuy = dashboard?.TiLeHuy ?? 0;

                DoanhThuTheoNgay.Clear();
                foreach (var item in revenues)
                    DoanhThuTheoNgay.Add(item);

                VeTheoNgay.Clear();
                foreach (var item in tickets)
                    VeTheoNgay.Add(item);

                UpdateCharts();
                RaiseSummaryProperties();
                StatusMessage = $"Đã tải thống kê từ {KhoangNgayText}.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải thống kê: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void DatLaiKhoangNgay()
        {
            TuNgay = DateTime.Today.AddDays(-14);
            DenNgay = DateTime.Today;
            StatusMessage = string.Empty;
        }

        private void UpdateCharts()
        {
            var revenueLabels = DoanhThuTheoNgay.Select(x => x.NgayText).ToArray();
            RevenueXAxes = new[]
            {
                new Axis
                {
                    Labels = revenueLabels,
                    LabelsRotation = 0,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(72, 101, 129))
                }
            };

            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Name = "Doanh thu",
                    Values = DoanhThuTheoNgay.Select(x => x.DoanhThu).ToArray(),
                    Fill = new SolidColorPaint(new SKColor(15, 108, 189)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(16, 42, 67)),
                    DataLabelsPosition = DataLabelsPosition.Top,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:N0}"
                }
            };

            var ticketLabels = VeTheoNgay.Select(x => x.NgayText).ToArray();
            TicketXAxes = new[]
            {
                new Axis
                {
                    Labels = ticketLabels,
                    LabelsRotation = 0,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(72, 101, 129))
                }
            };

            TicketSeries = new ISeries[]
            {
                new LineSeries<int>
                {
                    Name = "Tổng vé",
                    Values = VeTheoNgay.Select(x => x.TongVe).ToArray(),
                    Fill = null,
                    GeometrySize = 10,
                    Stroke = new SolidColorPaint(new SKColor(15, 108, 189), 3)
                },
                new LineSeries<int>
                {
                    Name = "Đã đặt",
                    Values = VeTheoNgay.Select(x => x.VeDaDat).ToArray(),
                    Fill = null,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(new SKColor(21, 128, 61), 3)
                },
                new LineSeries<int>
                {
                    Name = "Check-in",
                    Values = VeTheoNgay.Select(x => x.VeDaCheckin).ToArray(),
                    Fill = null,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(new SKColor(124, 58, 237), 3)
                },
                new LineSeries<int>
                {
                    Name = "Đã hủy",
                    Values = VeTheoNgay.Select(x => x.VeDaHuy).ToArray(),
                    Fill = null,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(new SKColor(220, 38, 38), 3)
                }
            };

            // Biểu đồ tròn - Tỷ lệ vé theo trạng thái
            var totalTickets = VeTheoNgay.Sum(x => x.TongVe);
            var totalBooked = VeTheoNgay.Sum(x => x.VeDaDat);
            var totalCheckedIn = VeTheoNgay.Sum(x => x.VeDaCheckin);
            var totalCancelled = VeTheoNgay.Sum(x => x.VeDaHuy);
            var totalUnused = totalTickets - totalBooked - totalCheckedIn - totalCancelled;

            TicketStatusPieSeries = new ISeries[]
            {
                new PieSeries<int>
                {
                    Name = "Đã đặt",
                    Values = new[] { totalBooked },
                    Fill = new SolidColorPaint(new SKColor(21, 128, 61)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}\n({(point.Coordinate.PrimaryValue / (double)totalTickets * 100):0.#}%)"
                },
                new PieSeries<int>
                {
                    Name = "Check-in",
                    Values = new[] { totalCheckedIn },
                    Fill = new SolidColorPaint(new SKColor(124, 58, 237)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}\n({(point.Coordinate.PrimaryValue / (double)totalTickets * 100):0.#}%)"
                },
                new PieSeries<int>
                {
                    Name = "Đã hủy",
                    Values = new[] { totalCancelled },
                    Fill = new SolidColorPaint(new SKColor(220, 38, 38)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}\n({(point.Coordinate.PrimaryValue / (double)totalTickets * 100):0.#}%)"
                },
                new PieSeries<int>
                {
                    Name = "Chưa sử dụng",
                    Values = new[] { totalUnused },
                    Fill = new SolidColorPaint(new SKColor(100, 116, 139)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}\n({(point.Coordinate.PrimaryValue / (double)totalTickets * 100):0.#}%)"
                }
            };

            // Biểu đồ cột - Top tuyến đường (mock data - cần API thực tế)
            var topRoutes = new[]
            {
                new { Route = "SGN → HAN", Count = 450 },
                new { Route = "HAN → SGN", Count = 420 },
                new { Route = "SGN → DAD", Count = 280 },
                new { Route = "DAD → SGN", Count = 260 },
                new { Route = "SGN → PQC", Count = 180 },
                new { Route = "HAN → DAD", Count = 150 },
                new { Route = "SGN → CXR", Count = 120 },
                new { Route = "HAN → PQC", Count = 100 }
            };

            TopRoutesXAxes = new[]
            {
                new Axis
                {
                    Labels = topRoutes.Select(x => x.Route).ToArray(),
                    LabelsRotation = 45,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(72, 101, 129))
                }
            };

            TopRoutesSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name = "Số vé bán",
                    Values = topRoutes.Select(x => x.Count).ToArray(),
                    Fill = new SolidColorPaint(new SKColor(15, 108, 189)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(16, 42, 67)),
                    DataLabelsPosition = DataLabelsPosition.Top,
                    DataLabelsSize = 12,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}"
                }
            };

            OnPropertyChanged(nameof(RevenueXAxes));
            OnPropertyChanged(nameof(RevenueSeries));
            OnPropertyChanged(nameof(TicketXAxes));
            OnPropertyChanged(nameof(TicketSeries));
            OnPropertyChanged(nameof(TicketStatusPieSeries));
            OnPropertyChanged(nameof(TopRoutesXAxes));
            OnPropertyChanged(nameof(TopRoutesSeries));
        }

        private Axis[] BuildRevenueYAxes()
        {
            return new[]
            {
                new Axis
                {
                    Labeler = value => $"{value / 1_000_000d:0.#}M",
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(72, 101, 129)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(223, 232, 242)) { StrokeThickness = 1 }
                }
            };
        }

        private Axis[] BuildTicketYAxes()
        {
            return new[]
            {
                new Axis
                {
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(72, 101, 129)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(223, 232, 242)) { StrokeThickness = 1 }
                }
            };
        }

        private Axis[] BuildTopRoutesYAxes()
        {
            return new[]
            {
                new Axis
                {
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(72, 101, 129)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(223, 232, 242)) { StrokeThickness = 1 }
                }
            };
        }

        private void RaiseSummaryProperties()
        {
            OnPropertyChanged(nameof(TongDoanhThuRangeText));
            OnPropertyChanged(nameof(TongVeRange));
            OnPropertyChanged(nameof(TongVeRangeText));
            OnPropertyChanged(nameof(KhoangNgayText));
            OnPropertyChanged(nameof(NgayDoanhThuCaoNhatText));
            OnPropertyChanged(nameof(NgayVeCaoNhatText));
            OnPropertyChanged(nameof(RevenueTrendText));
            OnPropertyChanged(nameof(TicketTrendText));
            OnPropertyChanged(nameof(RevenueTrendBrush));
            OnPropertyChanged(nameof(TicketTrendBrush));
        }

        public Task RefreshAsync() => TaiThongKeAsync();

        private string BuildRevenueTrendText()
        {
            var delta = GetRevenueTrendDelta();
            if (delta == null)
                return "Chưa đủ dữ liệu để so sánh 7 ngày.";

            if (delta.Value == 0)
                return "Doanh thu giữ ổn định so với 7 ngày trước.";

            return $"{(delta.Value > 0 ? "Tăng" : "Giảm")} {Math.Abs(delta.Value):0.#}% so với 7 ngày trước";
        }

        private string BuildTicketTrendText()
        {
            var delta = GetTicketTrendDelta();
            if (delta == null)
                return "Chưa đủ dữ liệu để so sánh lượng vé.";

            if (delta.Value == 0)
                return "Lượng vé giữ ổn định so với 7 ngày trước.";

            return $"{(delta.Value > 0 ? "Tăng" : "Giảm")} {Math.Abs(delta.Value):0.#}% lượng vé 7 ngày";
        }

        private double? GetRevenueTrendDelta()
            => ComputeDelta(
                DoanhThuTheoNgay.OrderByDescending(x => x.Ngay).Take(7).Sum(x => x.DoanhThu),
                DoanhThuTheoNgay.OrderByDescending(x => x.Ngay).Skip(7).Take(7).Sum(x => x.DoanhThu));

        private double? GetTicketTrendDelta()
            => ComputeDelta(
                VeTheoNgay.OrderByDescending(x => x.Ngay).Take(7).Sum(x => x.TongVe),
                VeTheoNgay.OrderByDescending(x => x.Ngay).Skip(7).Take(7).Sum(x => x.TongVe));

        private static double? ComputeDelta(decimal current, decimal previous)
        {
            if (current == 0 && previous == 0)
                return null;

            if (previous == 0)
                return 100;

            return (double)((current - previous) / previous * 100);
        }

        private static double? ComputeDelta(int current, int previous)
        {
            if (current == 0 && previous == 0)
                return null;

            if (previous == 0)
                return 100;

            return (double)(current - previous) / previous * 100;
        }

        private static Brush BuildTrendBrush(double? delta)
        {
            if (delta == null)
                return new SolidColorBrush(Color.FromRgb(72, 101, 129));

            if (delta < 0)
                return new SolidColorBrush(Color.FromRgb(214, 69, 69));

            return new SolidColorBrush(Color.FromRgb(21, 128, 61));
        }
    }
}


