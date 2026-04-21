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
    public class QuanLyPhieuGiamGiaViewModel : ObservableObject, IAdminRefreshable
    {
        private readonly QuanLyPhieuGiamGiaApi _api;

        public ObservableCollection<QuanLyPhieuGiamGiaItemDto> DanhSachPhieuGiamGia { get; } = new();

        public QuanLyPhieuGiamGiaViewModel(QuanLyPhieuGiamGiaApi api)
        {
            _api = api;

            LoadDanhSachCommand = new AsyncRelayCommand(LoadDanhSachAsync, () => !IsBusy);
            LuuPhieuGiamGiaCommand = new AsyncRelayCommand(LuuPhieuGiamGiaAsync, () => !IsBusy);
            DoiTrangThaiCommand = new AsyncRelayCommand(DoiTrangThaiAsync, () => !IsBusy && SelectedPhieuGiamGia != null);
            XoaPhieuGiamGiaCommand = new AsyncRelayCommand(XoaPhieuGiamGiaAsync, () => !IsBusy && SelectedPhieuGiamGia != null);
            OpenEditorCommand = new RelayCommand(OpenEditor);
            CreateNewCommand = new RelayCommand(CreateNew);
            CloseEditorCommand = new RelayCommand(CloseEditor);
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

        public int TongSoPhieu => DanhSachPhieuGiamGia.Count;

        private bool _isEditorOpen;
        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            set => SetProperty(ref _isEditorOpen, value);
        }

        private QuanLyPhieuGiamGiaItemDto? _selectedPhieuGiamGia;
        public QuanLyPhieuGiamGiaItemDto? SelectedPhieuGiamGia
        {
            get => _selectedPhieuGiamGia;
            set
            {
                if (SetProperty(ref _selectedPhieuGiamGia, value))
                {
                    RaiseCommandStates();
                    if (value != null)
                    {
                        NapFormTuDanhSach(value);
                        IsEditorOpen = true;
                    }
                }
            }
        }

        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _maGiamGia = string.Empty;
        public string MaGiamGia
        {
            get => _maGiamGia;
            set => SetProperty(ref _maGiamGia, value);
        }

        private string _giaTriGiam = string.Empty;
        public string GiaTriGiam
        {
            get => _giaTriGiam;
            set => SetProperty(ref _giaTriGiam, value);
        }

        private DateTime? _ngayKetThuc = DateTime.Today;
        public DateTime? NgayKetThuc
        {
            get => _ngayKetThuc;
            set => SetProperty(ref _ngayKetThuc, value);
        }

        private string _noiDung = string.Empty;
        public string NoiDung
        {
            get => _noiDung;
            set => SetProperty(ref _noiDung, value);
        }

        private bool _active = true;
        public bool Active
        {
            get => _active;
            set => SetProperty(ref _active, value);
        }

        private string _idLoaiGiamGia = "1";
        public string IdLoaiGiamGia
        {
            get => _idLoaiGiamGia;
            set => SetProperty(ref _idLoaiGiamGia, value);
        }

        public AsyncRelayCommand LoadDanhSachCommand { get; }
        public AsyncRelayCommand LuuPhieuGiamGiaCommand { get; }
        public AsyncRelayCommand DoiTrangThaiCommand { get; }
        public AsyncRelayCommand XoaPhieuGiamGiaCommand { get; }
        public RelayCommand OpenEditorCommand { get; }
        public RelayCommand CreateNewCommand { get; }
        public RelayCommand CloseEditorCommand { get; }
        public RelayCommand ResetFormCommand { get; }

        private async Task LoadDanhSachAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var data = await _api.GetDanhSachPhieuGiamGiaAsync();

                DanhSachPhieuGiamGia.Clear();
                foreach (var item in data.OrderByDescending(x => x.Id))
                    DanhSachPhieuGiamGia.Add(item);

                if (DanhSachPhieuGiamGia.Count == 0)
                {
                    SelectedPhieuGiamGia = null;
                    StatusMessage = "Chưa có phiếu giảm giá nào.";
                    return;
                }

                SelectedPhieuGiamGia ??= DanhSachPhieuGiamGia.FirstOrDefault();
                OnPropertyChanged(nameof(TongSoPhieu));
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi tải danh sách phiếu giảm giá: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LuuPhieuGiamGiaAsync()
        {
            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var payload = BuildPayload();
                var result = await _api.CapNhatPhieuGiamGiaAsync(payload);
                StatusMessage = result.Message ?? "Lưu phiếu giảm giá thành công.";
                await LoadDanhSachAsync();
                ChonLaiPhieu(payload.Id, payload.MaGiamGia);
                IsEditorOpen = false;
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi lưu phiếu giảm giá: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DoiTrangThaiAsync()
        {
            if (SelectedPhieuGiamGia == null) return;

            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = await _api.ActivePhieuGiamGiaAsync(SelectedPhieuGiamGia.Id);
                StatusMessage = result.Message ?? "Cập nhật trạng thái thành công.";
                await LoadDanhSachAsync();
                ChonLaiPhieu(SelectedPhieuGiamGia.Id, SelectedPhieuGiamGia.MaGiamGia);
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi đổi trạng thái phiếu giảm giá: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task XoaPhieuGiamGiaAsync()
        {
            if (SelectedPhieuGiamGia == null) return;
            if (!AdminUiService.ConfirmDelete("Xóa phiếu giảm giá này?")) return;

            StatusMessage = string.Empty;
            IsBusy = true;

            try
            {
                var result = await _api.XoaPhieuGiamGiaAsync(SelectedPhieuGiamGia.Id);
                StatusMessage = result.Message ?? "Xóa phiếu giảm giá thành công.";
                ResetForm();
                await LoadDanhSachAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi xóa phiếu giảm giá: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetForm()
        {
            Id = string.Empty;
            MaGiamGia = string.Empty;
            GiaTriGiam = string.Empty;
            NgayKetThuc = DateTime.Today;
            NoiDung = string.Empty;
            Active = true;
            IdLoaiGiamGia = "1";
            SelectedPhieuGiamGia = null;
            IsEditorOpen = false;
            StatusMessage = string.Empty;
        }

        private void OpenEditor()
        {
            if (SelectedPhieuGiamGia == null && string.IsNullOrWhiteSpace(Id))
                ResetForm();

            IsEditorOpen = true;
        }

        private void CreateNew()
        {
            ResetForm();
            IsEditorOpen = true;
        }

        private void CloseEditor()
        {
            IsEditorOpen = false;
        }

        private QuanLyPhieuGiamGiaUpsertModel BuildPayload()
        {
            if (!decimal.TryParse(GiaTriGiam, NumberStyles.Any, CultureInfo.InvariantCulture, out var giaTri))
                throw new Exception("Giá trị giảm không hợp lệ.");

            if (!int.TryParse(IdLoaiGiamGia, out var idLoaiGiamGia))
                throw new Exception("ID loại giảm giá không hợp lệ.");

            if (!NgayKetThuc.HasValue)
                throw new Exception("Bạn cần chọn ngày kết thúc.");

            return new QuanLyPhieuGiamGiaUpsertModel
            {
                Id = ParseLong(Id),
                MaGiamGia = (MaGiamGia ?? string.Empty).Trim(),
                GiaTriGiam = giaTri,
                NgayKetThuc = DateOnly.FromDateTime(NgayKetThuc.Value),
                NoiDung = (NoiDung ?? string.Empty).Trim(),
                Active = Active,
                IdLoaiGiamGia = idLoaiGiamGia
            };
        }

        private void NapFormTuDanhSach(QuanLyPhieuGiamGiaItemDto item)
        {
            Id = item.Id.ToString();
            MaGiamGia = item.MaGiamGia;
            GiaTriGiam = item.GiaTriGiam.ToString(CultureInfo.InvariantCulture);
            NgayKetThuc = item.NgayKetThuc.ToDateTime(TimeOnly.MinValue);
            NoiDung = item.NoiDung ?? string.Empty;
            Active = item.Active ?? false;
        }

        private void ChonLaiPhieu(long id, string maGiamGia)
        {
            SelectedPhieuGiamGia = DanhSachPhieuGiamGia.FirstOrDefault(x => x.Id == id)
                ?? DanhSachPhieuGiamGia.FirstOrDefault(x => string.Equals(x.MaGiamGia, maGiamGia, StringComparison.OrdinalIgnoreCase));
        }

        private static long ParseLong(string? text)
        {
            return long.TryParse(text, out var value) ? value : 0;
        }

        public Task RefreshAsync() => LoadDanhSachAsync();

        private void RaiseCommandStates()
        {
            LoadDanhSachCommand.RaiseCanExecuteChanged();
            LuuPhieuGiamGiaCommand.RaiseCanExecuteChanged();
            DoiTrangThaiCommand.RaiseCanExecuteChanged();
            XoaPhieuGiamGiaCommand.RaiseCanExecuteChanged();
        }
    }
}





