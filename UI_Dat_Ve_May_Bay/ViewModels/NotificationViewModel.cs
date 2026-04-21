using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using UI_Dat_Ve_May_Bay.Api;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Models.Notifications;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class NotificationViewModel : ObservableObject
    {
        private readonly NotificationApi _api;

        public ObservableCollection<NotificationDto> Notifications { get; } = new();

        private NotificationDto? _selected;
        public NotificationDto? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    ((RelayCommand)ViewDetailCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();

                    // auto load detail khi click item
                    _ = LoadDetailAsync();
                }
            }
        }

        // Detail dùng lại NotificationDto
        private NotificationDto? _selectedDetail;
        public NotificationDto? SelectedDetail
        {
            get => _selectedDetail;
            set => SetProperty(ref _selectedDetail, value);
        }

        private string _status = "Sẵn sàng.";
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

        public bool HasStatus => !string.IsNullOrWhiteSpace(Status) && Status != "Sẵn sàng.";
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        public AsyncRelayCommand RefreshCommand { get; }
        public RelayCommand ViewDetailCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        private bool _isLoadingDetail = false;

        public NotificationViewModel(NotificationApi api)
        {
            _api = api;

            RefreshCommand = new AsyncRelayCommand(LoadAsync);

            ViewDetailCommand = new RelayCommand(
                async () => await LoadDetailAsync(),
                canExecute: () => Selected != null
            );

            DeleteCommand = new AsyncRelayCommand(
                DeleteSelectedAsync,
                canExecute: () => Selected != null
            );

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                Status = "Đang tải danh sách thông báo...";
                var list = await _api.GetThongBaoAsync();

                Notifications.Clear();
                foreach (var item in list)
                    Notifications.Add(item);

                Status = $"Tải xong: {Notifications.Count} thông báo.";
                Error = "";

                // reset detail khi reload
                SelectedDetail = null;
            }
            catch (Exception ex)
            {
                Status = "Lỗi tải thông báo.";
                Error = ex.Message;
                // MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDetailAsync()
        {
            if (Selected == null) return;

            try
            {
                Status = "Đang tải chi tiết...";

                var detail = await _api.GetChiTietThongBaoAsync(Selected.Id);
                SelectedDetail = detail;

                Status = "Tải chi tiết xong.";
                Error = "";
            }
            catch (Exception ex)
            {
                Status = "Lỗi tải chi tiết.";
                Error = ex.Message;
            }
        }


        private async Task DeleteSelectedAsync()
        {
            if (Selected == null) return;

            var id = Selected.Id;

            if (!UI_Dat_Ve_May_Bay.Services.DialogService.ShowConfirm("Xóa thông báo này?", "Xác nhận"))
                return;

            try
            {
                Status = "Đang xóa...";
                await _api.XoaThongBaoAsync(id);

                // nếu đang xem detail của item bị xóa thì clear
                if (SelectedDetail != null && SelectedDetail.Id == id)
                    SelectedDetail = null;

                Status = "Đã xóa. Đang tải lại...";
                Error = "";
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Status = "Lỗi xóa.";
                Error = ex.Message;
            }
        }
    }
}
