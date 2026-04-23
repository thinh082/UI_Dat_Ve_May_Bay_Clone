using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Models.Vouchers;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class VoucherViewModel : ObservableObject
    {
        private readonly VoucherApi _voucherApi;

        public ObservableCollection<VoucherDto> Vouchers { get; } = new();
        private readonly List<VoucherDto> _cachedVouchers = new();

        private VoucherDto? _selectedVoucher;
        public VoucherDto? SelectedVoucher
        {
            get => _selectedVoucher;
            set => SetProperty(ref _selectedVoucher, value);
        }

        private string _searchCode = "";
        public string SearchCode
        {
            get => _searchCode;
            set => SetProperty(ref _searchCode, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
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

        public bool HasStatus => !string.IsNullOrWhiteSpace(Status);
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        public AsyncRelayCommand LoadMyVouchersCommand { get; }
        public AsyncRelayCommand LoadAllVouchersCommand { get; }   // ✅ phải init
        public AsyncRelayCommand SearchCommand { get; }
        public AsyncRelayCommand ApplySelectedCommand { get; }
        public AsyncRelayCommand LoadDetailsCommand { get; }

        public VoucherViewModel(VoucherApi voucherApi, bool autoLoad = false)
        {
            _voucherApi = voucherApi;

            LoadMyVouchersCommand = new AsyncRelayCommand(LoadMyVouchersAsync);
            LoadAllVouchersCommand = new AsyncRelayCommand(LoadAllVouchersAsync); // ✅ FIX: gán command
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ApplySelectedCommand = new AsyncRelayCommand(ApplySelectedAsync);
            LoadDetailsCommand = new AsyncRelayCommand(LoadDetailsAsync);

            // ✅ chỉ auto-load khi thật sự điều hướng vào tab Voucher
            if (autoLoad)
                _ = LoadMyVouchersAsync();
        }

        private async Task LoadMyVouchersAsync()
        {
            await RunSafe(async () =>
            {
                Status = "Đang tải voucher của tôi...";
                Vouchers.Clear();

                var list = await _voucherApi.LayToanBoPhieuGiamGiaAsync();
                _cachedVouchers.Clear();
                foreach (var v in list)
                {
                    Vouchers.Add(v);
                    _cachedVouchers.Add(v);
                }

                Status = Vouchers.Count == 0 ? "Không có voucher." : $"Đã tải {Vouchers.Count} voucher.";
                Error = "";
                
                // ✅ FIX: Hiển thị dialog thành công
                if (Vouchers.Count > 0)
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowSuccess($"Đã tải {Vouchers.Count} voucher.", "Tải thành công");
            });
        }

        private async Task LoadAllVouchersAsync()
        {
            await RunSafe(async () =>
            {
                Status = "Đang tải tất cả voucher...";
                Vouchers.Clear();

                var list = await _voucherApi.LayToanBoPhieuGiamGiaAsync(); // ✅ Đổi sang gọi LayToanBoPhieuGiamGia theo yêu cầu
                _cachedVouchers.Clear();
                foreach (var v in list)
                {
                    Vouchers.Add(v);
                    _cachedVouchers.Add(v);
                }

                Status = Vouchers.Count == 0 ? "Không có voucher." : $"Đã tải {Vouchers.Count} voucher.";
                Error = "";
                
                // ✅ FIX: Hiển thị dialog thành công
                if (Vouchers.Count > 0)
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowSuccess($"Đã tải {Vouchers.Count} voucher.", "Tải thành công");
            });
        }

        private async Task SearchAsync()
        {
            await RunSafe(async () =>
            {
                // ✅ FIX: Clear status trước khi kiểm tra input
                Status = "";
                Error = "";
                
                if (string.IsNullOrWhiteSpace(SearchCode))
                {
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowWarning("Nhập mã giảm giá để tìm.", "Thiếu dữ liệu");
                    return;
                }

                Status = "Đang tìm mã giảm giá...";
                var code = SearchCode.Trim();

                // 1. Thử lọc trong cache trước (cho nhanh và chính xác với những gì đang thấy)
                var localMatches = _cachedVouchers.FindAll(v =>
                    (v.MaGiamGia ?? "").Equals(code, StringComparison.OrdinalIgnoreCase));

                if (localMatches.Count > 0)
                {
                    Vouchers.Clear();
                    foreach (var m in localMatches) Vouchers.Add(m);
                    Status = $"Tìm thấy {Vouchers.Count} voucher (local).";
                    Error = "";
                    // ✅ FIX: Hiển thị dialog thành công
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowSuccess($"Tìm thấy {Vouchers.Count} voucher.", "Tìm kiếm thành công");
                    return;
                }

                // 2. Nếu không thấy local, mới gọi API (tìm mã mới chưa tải)
                Vouchers.Clear();
                var list = await _voucherApi.TimKiemMaGiamGiaAsync(code);
                foreach (var v in list) Vouchers.Add(v);

                Status = Vouchers.Count == 0 ? "Không tìm thấy voucher." : $"Tìm thấy {Vouchers.Count} voucher.";
                Error = "";
                
                // ✅ FIX: Hiển thị dialog kết quả
                if (Vouchers.Count > 0)
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowSuccess($"Tìm thấy {Vouchers.Count} voucher.", "Tìm kiếm thành công");
                else
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowWarning("Không tìm thấy voucher với mã này.", "Không có kết quả");
            });
        }

        private async Task ApplySelectedAsync()
        {
            await RunSafe(async () =>
            {
                if (SelectedVoucher == null)
                {
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowWarning("Chọn 1 voucher trước.", "Thiếu dữ liệu");
                    return;
                }

                var id = SelectedVoucher.Id; // ✅ dùng Id theo JSON
                if (id <= 0)
                {
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowError("Voucher không có Id hợp lệ.", "Lỗi dữ liệu");
                    return;
                }

                Status = "Đang áp dụng voucher...";
                var res = await _voucherApi.ApplyVoucherAsync(id);

                // ✅ FIX: Hiển thị dialog thành công
                UI_Dat_Ve_May_Bay.Services.DialogService.ShowSuccess(res.Message ?? "Áp dụng voucher thành công!", "Thành công");

                // Sau khi apply, thường hợp lý là load lại voucher của tôi
                await LoadMyVouchersAsync();
            });
        }

        private async Task LoadDetailsAsync()
        {
            await RunSafe(async () =>
            {
                Status = "Đang tải chi tiết voucher...";
                Vouchers.Clear();

                var list = await _voucherApi.LayDanhSachChiTietPhieuGiamGiaAsync();
                foreach (var v in list) Vouchers.Add(v);

                Status = Vouchers.Count == 0 ? "Không có chi tiết voucher." : $"Đã tải {Vouchers.Count} dòng chi tiết.";
                Error = "";
                
                // ✅ FIX: Hiển thị dialog thành công
                if (Vouchers.Count > 0)
                    UI_Dat_Ve_May_Bay.Services.DialogService.ShowSuccess($"Đã tải {Vouchers.Count} chi tiết voucher.", "Tải thành công");
            });
        }

        private async Task RunSafe(Func<Task> action)
        {
            try
            {
                IsLoading = true;
                await action();
            }
            catch (Exception ex)
            {
                Status = "Lỗi.";
                Error = ex.Message;
                // ✅ FIX: Hiển thị dialog lỗi
                UI_Dat_Ve_May_Bay.Services.DialogService.ShowError(ex.Message, "Lỗi");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}