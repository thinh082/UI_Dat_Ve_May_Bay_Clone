using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Api.Admin;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Models.Admin;

namespace UI_Dat_Ve_May_Bay.ViewModels.Admin
{
    public class QuanLyChuyenBayViewModel : ObservableObject, IAdminRefreshable
    {
        private readonly QuanLyChuyenBayApi _api;

        public ObservableCollection<QuanLyChuyenBayItemDto> DanhSachChuyenBay { get; } = new();
        public ObservableCollection<QuanLyChuyenBayLichBayDto> LichBayHomNay { get; } = new();
        public ObservableCollection<QuanLyChuyenBayTienNghiDto> TienNghi { get; } = new();

        public QuanLyChuyenBayViewModel(QuanLyChuyenBayApi api)
        {
            _api = api;

            LoadDanhSachCommand = new AsyncRelayCommand(LoadDanhSachAsync, () => !IsBusy);
            XemChiTietCommand = new AsyncRelayCommand(XemChiTietAsync, () => !IsBusy && SelectedChuyenBay != null);
            LuuChuyenBayCommand = new AsyncRelayCommand(LuuChuyenBayAsync, () => !IsBusy);
            XoaChuyenBayCommand = new AsyncRelayCommand(XoaChuyenBayAsync, () => !IsBusy && SelectedChuyenBay != null);
            OpenEditorCommand = new RelayCommand(OpenEditor);
            CloseEditorCommand = new RelayCommand(CloseEditor);
            ResetBoLocCommand = new RelayCommand(ResetBoLoc);
            ResetFormCommand = new RelayCommand(ResetForm);

            _ = LoadDanhSachAsync();
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

        public int TongSoChuyenBay => DanhSachChuyenBay.Count;
        public string FilterBadgeText => $"{TongSoChuyenBay} chuyến bay";

        private bool _isEditorOpen;
        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            set => SetProperty(ref _isEditorOpen, value);
        }

        private QuanLyChuyenBayItemDto? _selectedChuyenBay;
        public QuanLyChuyenBayItemDto? SelectedChuyenBay
        {
            get => _selectedChuyenBay;
            set
            {
                if (SetProperty(ref _selectedChuyenBay, value))
                {
                    RaiseCommandStates();
                    OnPropertyChanged(nameof(SelectedChuyenBayLabel));
                    if (value != null)
                    {
                        NapForm(value);
                        IsEditorOpen = true;
                        _ = XemChiTietAsync();
                    }
                }
            }
        }

        private QuanLyChuyenBayDetailDto? _chiTietChuyenBay;
        public QuanLyChuyenBayDetailDto? ChiTietChuyenBay
        {
            get => _chiTietChuyenBay;
            set
            {
                if (SetProperty(ref _chiTietChuyenBay, value))
                    OnPropertyChanged(nameof(SelectedHanhTrinhText));
            }
        }

        public string SelectedChuyenBayLabel => SelectedChuyenBay != null ? $"CB #{SelectedChuyenBay.Id}" : "Chưa chọn chuyến bay";
        public string SelectedHanhTrinhText => ChiTietChuyenBay?.HanhTrinhText ?? SelectedChuyenBay?.HanhTrinhText ?? "--";

        private string _maSanBayDi = string.Empty;
        public string MaSanBayDi
        {
            get => _maSanBayDi;
            set => SetProperty(ref _maSanBayDi, value);
        }

        private string _maSanBayDen = string.Empty;
        public string MaSanBayDen
        {
            get => _maSanBayDen;
            set => SetProperty(ref _maSanBayDen, value);
        }

        private string _tenHangBay = string.Empty;
        public string TenHangBay
        {
            get => _tenHangBay;
            set => SetProperty(ref _tenHangBay, value);
        }

        private string _giaMin = string.Empty;
        public string GiaMin
        {
            get => _giaMin;
            set => SetProperty(ref _giaMin, value);
        }

        private string _giaMax = string.Empty;
        public string GiaMax
        {
            get => _giaMax;
            set => SetProperty(ref _giaMax, value);
        }

        private string _formIdChuyenBay = string.Empty;
        public string FormIdChuyenBay
        {
            get => _formIdChuyenBay;
            set => SetProperty(ref _formIdChuyenBay, value);
        }

        private string _formIdHangBay = string.Empty;
        public string FormIdHangBay
        {
            get => _formIdHangBay;
            set => SetProperty(ref _formIdHangBay, value);
        }

        private string _formMaSanBayDi = string.Empty;
        public string FormMaSanBayDi
        {
            get => _formMaSanBayDi;
            set => SetProperty(ref _formMaSanBayDi, value);
        }

        private string _formMaSanBayDen = string.Empty;
        public string FormMaSanBayDen
        {
            get => _formMaSanBayDen;
            set => SetProperty(ref _formMaSanBayDen, value);
        }

        public AsyncRelayCommand LoadDanhSachCommand { get; }
        public AsyncRelayCommand XemChiTietCommand { get; }
        public AsyncRelayCommand LuuChuyenBayCommand { get; }
        public AsyncRelayCommand XoaChuyenBayCommand { get; }
        public RelayCommand OpenEditorCommand { get; }
        public RelayCommand CloseEditorCommand { get; }
        public RelayCommand ResetBoLocCommand { get; }
        public RelayCommand ResetFormCommand { get; }

        private async Task LoadDanhSachAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var data = await _api.GetDanhSachChuyenBayAsync(new QuanLyChuyenBayFilterModel
                {
                    MaSanBayDi = EmptyToNull(MaSanBayDi),
                    MaSanBayDen = EmptyToNull(MaSanBayDen),
                    TenHangBay = EmptyToNull(TenHangBay),
                    GiaMin = ParseDecimal(GiaMin),
                    GiaMax = ParseDecimal(GiaMax)
                });

                DanhSachChuyenBay.Clear();
                foreach (var item in data.OrderBy(x => x.Id))
                    DanhSachChuyenBay.Add(item);

                OnPropertyChanged(nameof(TongSoChuyenBay));
                OnPropertyChanged(nameof(FilterBadgeText));

                if (DanhSachChuyenBay.Count == 0)
                {
                    SelectedChuyenBay = null;
                    ChiTietChuyenBay = null;
                    LichBayHomNay.Clear();
                    TienNghi.Clear();
                    StatusMessage = "Không tìm thấy chuyến bay phù hợp.";
                }
                else
                {
                    SelectedChuyenBay ??= DanhSachChuyenBay.FirstOrDefault();
                    StatusMessage = $"Đã tải {DanhSachChuyenBay.Count} chuyến bay.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải danh sách chuyến bay: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task XemChiTietAsync()
        {
            if (SelectedChuyenBay == null) return;

            try
            {
                ChiTietChuyenBay = await _api.GetChiTietChuyenBayAsync(SelectedChuyenBay.Id);
                LichBayHomNay.Clear();
                foreach (var item in ChiTietChuyenBay?.LichBayHomNay ?? Enumerable.Empty<QuanLyChuyenBayLichBayDto>())
                    LichBayHomNay.Add(item);

                TienNghi.Clear();
                foreach (var item in ChiTietChuyenBay?.TienNghi ?? Enumerable.Empty<QuanLyChuyenBayTienNghiDto>())
                    TienNghi.Add(item);
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải chi tiết chuyến bay: " + ex.Message;
            }
        }

        private async Task LuuChuyenBayAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var payload = new QuanLyChuyenBaySaveModel
                {
                    IdChuyenBay = ParseLong(FormIdChuyenBay),
                    IdHangBay = ParseInt(FormIdHangBay),
                    MaSanBayDi = (FormMaSanBayDi ?? string.Empty).Trim().ToUpperInvariant(),
                    MaSanBayDen = (FormMaSanBayDen ?? string.Empty).Trim().ToUpperInvariant()
                };

                var result = await _api.LuuChuyenBayAsync(payload);
                StatusMessage = result.Message ?? "Đã lưu chuyến bay thành công.";
                await LoadDanhSachAsync();

                if (payload.IdChuyenBay > 0)
                    SelectedChuyenBay = DanhSachChuyenBay.FirstOrDefault(x => x.Id == payload.IdChuyenBay);

                IsEditorOpen = false;
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi lưu chuyến bay: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task XoaChuyenBayAsync()
        {
            if (SelectedChuyenBay == null) return;
            if (!AdminUiService.ConfirmDelete("Xóa chuyến bay này?")) return;

            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = await _api.XoaChuyenBayAsync(SelectedChuyenBay.Id);
                StatusMessage = result.Message ?? "Đã xóa chuyến bay.";
                ResetForm();
                await LoadDanhSachAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi xóa chuyến bay: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetBoLoc()
        {
            MaSanBayDi = string.Empty;
            MaSanBayDen = string.Empty;
            TenHangBay = string.Empty;
            GiaMin = string.Empty;
            GiaMax = string.Empty;
            StatusMessage = string.Empty;
        }

        private void ResetForm()
        {
            FormIdChuyenBay = string.Empty;
            FormIdHangBay = string.Empty;
            FormMaSanBayDi = string.Empty;
            FormMaSanBayDen = string.Empty;
            SelectedChuyenBay = null;
            ChiTietChuyenBay = null;
            LichBayHomNay.Clear();
            TienNghi.Clear();
            IsEditorOpen = false;
        }

        private void NapForm(QuanLyChuyenBayItemDto item)
        {
            FormIdChuyenBay = item.Id.ToString();
            FormIdHangBay = item.IdHangBay.ToString();
            FormMaSanBayDi = item.MaSanBayDi ?? string.Empty;
            FormMaSanBayDen = item.MaSanBayDen ?? string.Empty;
        }

        private static decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out value)
                    ? value
                    : null;
        }

        private static string? EmptyToNull(string text)
            => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static long ParseLong(string text)
            => long.TryParse(text, out var value) ? value : 0;

        private static int ParseInt(string text)
            => int.TryParse(text, out var value) ? value : 0;

        public Task RefreshAsync() => LoadDanhSachAsync();

        private void OpenEditor()
        {
            if (SelectedChuyenBay == null && string.IsNullOrWhiteSpace(FormIdChuyenBay))
                ResetForm();

            IsEditorOpen = true;
        }

        private void CloseEditor()
        {
            IsEditorOpen = false;
        }

        private void RaiseCommandStates()
        {
            LoadDanhSachCommand.RaiseCanExecuteChanged();
            XemChiTietCommand.RaiseCanExecuteChanged();
            LuuChuyenBayCommand.RaiseCanExecuteChanged();
            XoaChuyenBayCommand.RaiseCanExecuteChanged();
        }
    }
}





